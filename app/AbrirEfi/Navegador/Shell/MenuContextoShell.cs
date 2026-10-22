using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using AbrirEfi.Comum;
using AbrirEfi.Comum.Nativo;

namespace AbrirEfi.Navegador.Shell
{
    /// <summary>
    /// Mostra o menu de contexto REAL do Windows (o de "Mostrar mais opções") para arquivos/pastas.
    /// Fluxo: SHParseDisplayName → SHBindToParent → IShellFolder.GetUIObjectOf(IContextMenu) →
    /// QueryContextMenu → TrackPopupMenuEx(TPM_RETURNCMD) → InvokeCommand.
    /// Enquanto o menu está aberto, o formulário dono repassa WM_INITMENUPOPUP/WM_DRAWITEM/WM_MEASUREITEM/
    /// WM_MENUCHAR para <see cref="TentarTratar"/> (necessário para submenus como "Enviar para" e para os
    /// itens desenhados pelo próprio programa).
    /// RISCO: extensões de terceiros do menu (7-Zip, antivírus...) são carregadas dentro deste processo
    /// elevado; uma delas com defeito pode derrubar o app (o try/catch não pega falha nativa).
    /// </summary>
    internal sealed class MenuContextoShell
    {
        private const uint IdPrimeiro = 1;
        private const uint IdUltimo = 0x7FFF;

        private static readonly Guid IidShellFolder = new Guid("000214E6-0000-0000-C000-000000000046");
        private static readonly Guid IidContextMenu = new Guid("000214E4-0000-0000-C000-000000000046");

        private static MenuContextoShell _atual;

        private IContextMenu2 _menu2;
        private IContextMenu3 _menu3;

        /// <summary>
        /// Chamado pelo WndProc do formulário dono. Devolve true (e define m.Result) se a mensagem foi
        /// tratada pelo menu do shell que está aberto.
        /// </summary>
        public static bool TentarTratar(ref Message m)
        {
            var atual = _atual;
            if (atual == null) return false;
            switch ((uint)m.Msg)
            {
                case NativoJanelas.WM_INITMENUPOPUP:
                case NativoJanelas.WM_DRAWITEM:
                case NativoJanelas.WM_MEASUREITEM:
                case NativoJanelas.WM_MENUCHAR:
                    return atual.Repassar(ref m);
                default:
                    return false;
            }
        }

        /// <summary>
        /// Abre o menu na posição da tela e executa o comando escolhido.
        /// Devolve false se não foi possível montar o menu (o motivo vai para o log).
        /// </summary>
        public static bool Mostrar(Form dono, IList<string> caminhos, Point posicaoTela, bool verbosEstendidos)
        {
            if (caminhos == null || caminhos.Count == 0) return false;
            var instancia = new MenuContextoShell();
            return instancia.Executar(dono, caminhos, posicaoTela, verbosEstendidos);
        }

        private bool Executar(Form dono, IList<string> caminhos, Point posicaoTela, bool verbosEstendidos)
        {
            var pidlsCompletos = new List<IntPtr>();
            IShellFolder pasta = null;
            IContextMenu menu = null;
            var hmenu = IntPtr.Zero;
            try
            {
                var filhos = new IntPtr[caminhos.Count];
                for (var i = 0; i < caminhos.Count; i++)
                {
                    IntPtr pidl;
                    uint atributos;
                    var hr = NativoMenuShell.SHParseDisplayName(caminhos[i], IntPtr.Zero, out pidl, 0, out atributos);
                    if (hr < 0 || pidl == IntPtr.Zero) { Log.Escrever("Menu shell: SHParseDisplayName falhou 0x{0:X} ({1})", hr, caminhos[i]); return false; }
                    pidlsCompletos.Add(pidl);

                    var iid = IidShellFolder;
                    IntPtr ppv, filho;
                    hr = NativoMenuShell.SHBindToParent(pidl, ref iid, out ppv, out filho);
                    if (hr < 0) { Log.Escrever("Menu shell: SHBindToParent falhou 0x{0:X}", hr); return false; }
                    filhos[i] = filho; // aponta para dentro de pidl: não liberar separado

                    if (i == 0) pasta = (IShellFolder)Marshal.GetObjectForIUnknown(ppv); // todos os itens estão na mesma pasta
                    Marshal.Release(ppv);
                }

                var iidMenu = IidContextMenu;
                IntPtr ppvMenu;
                var r = pasta.GetUIObjectOf(dono.Handle, (uint)filhos.Length, filhos, ref iidMenu, IntPtr.Zero, out ppvMenu);
                if (r < 0 || ppvMenu == IntPtr.Zero) { Log.Escrever("Menu shell: GetUIObjectOf falhou 0x{0:X}", r); return false; }
                menu = (IContextMenu)Marshal.GetObjectForIUnknown(ppvMenu);
                Marshal.Release(ppvMenu);
                _menu2 = menu as IContextMenu2; // o cast faz QueryInterface
                _menu3 = menu as IContextMenu3;

                hmenu = NativoMenuShell.CreatePopupMenu();
                var flags = NativoMenuShell.CMF_NORMAL | NativoMenuShell.CMF_EXPLORE;
                if (verbosEstendidos) flags |= NativoMenuShell.CMF_EXTENDEDVERBS;
                r = menu.QueryContextMenu(hmenu, 0, IdPrimeiro, IdUltimo, flags);
                if (r < 0) { Log.Escrever("Menu shell: QueryContextMenu falhou 0x{0:X}", r); return false; }

                _atual = this;
                NativoJanelas.SetForegroundWindow(dono.Handle);
                var escolhido = NativoMenuShell.TrackPopupMenuEx(hmenu,
                    NativoMenuShell.TPM_RETURNCMD | NativoMenuShell.TPM_RIGHTBUTTON,
                    posicaoTela.X, posicaoTela.Y, dono.Handle, IntPtr.Zero);
                _atual = null;
                NativoJanelas.PostMessage(dono.Handle, NativoJanelas.WM_NULL, IntPtr.Zero, IntPtr.Zero);

                if (escolhido > 0)
                {
                    var deslocamento = (IntPtr)(escolhido - (int)IdPrimeiro);
                    var info = new NativoMenuShell.CMINVOKECOMMANDINFOEX
                    {
                        fMask = NativoMenuShell.CMIC_MASK_UNICODE | NativoMenuShell.CMIC_MASK_PTINVOKE,
                        hwnd = dono.Handle,
                        lpVerb = deslocamento,   // MAKEINTRESOURCEA(idCmd - idCmdFirst)
                        lpVerbW = deslocamento,  // MAKEINTRESOURCEW(...)
                        nShow = 1,               // SW_SHOWNORMAL
                        ptInvoke = new PontoNativo { X = posicaoTela.X, Y = posicaoTela.Y },
                    };
                    info.cbSize = (uint)Marshal.SizeOf(typeof(NativoMenuShell.CMINVOKECOMMANDINFOEX));
                    r = menu.InvokeCommand(ref info);
                    Log.Escrever("Menu shell: comando {0} executado, HRESULT 0x{1:X}", escolhido - (int)IdPrimeiro, r);
                }
                return true;
            }
            finally
            {
                _atual = null;
                if (hmenu != IntPtr.Zero) NativoMenuShell.DestroyMenu(hmenu);
                _menu2 = null;
                _menu3 = null;
                if (menu != null) Marshal.ReleaseComObject(menu);
                if (pasta != null) Marshal.ReleaseComObject(pasta);
                foreach (var pidl in pidlsCompletos) NativoMenuShell.ILFree(pidl);
            }
        }

        private bool Repassar(ref Message m)
        {
            try
            {
                if (_menu3 != null)
                {
                    IntPtr resultado;
                    var hr = _menu3.HandleMenuMsg2((uint)m.Msg, m.WParam, m.LParam, out resultado);
                    if (hr < 0) return false;
                    m.Result = ResultadoPadrao((uint)m.Msg, resultado);
                    return true;
                }
                if (_menu2 != null)
                {
                    var hr = _menu2.HandleMenuMsg((uint)m.Msg, m.WParam, m.LParam);
                    if (hr < 0) return false;
                    m.Result = ResultadoPadrao((uint)m.Msg, IntPtr.Zero);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log.Erro("repassar mensagem ao menu do shell", ex);
            }
            return false;
        }

        // WM_INITMENUPOPUP devolve 0; WM_DRAWITEM/WM_MEASUREITEM devolvem TRUE; WM_MENUCHAR usa o resultado do shell.
        private static IntPtr ResultadoPadrao(uint mensagem, IntPtr resultadoShell)
        {
            switch (mensagem)
            {
                case NativoJanelas.WM_INITMENUPOPUP: return IntPtr.Zero;
                case NativoJanelas.WM_MENUCHAR: return resultadoShell;
                default: return (IntPtr)1;
            }
        }
    }
}
