using System;
using System.Runtime.InteropServices;

namespace AbrirEfi.Navegador.Shell
{
    // Interfaces COM do shell, com os métodos NA ORDEM EXATA da vtable do SDK (shobjidl_core.h / shlobj_core.h).
    // Cada interface repete os métodos das que herda, em vez de usar herança do C#, para a ordem ficar explícita.
    // Todos os métodos usam PreserveSig: o HRESULT volta como int e quem chama decide o que fazer.

    /// <summary>IShellFolder (000214E6-0000-0000-C000-000000000046).</summary>
    [ComImport, Guid("000214E6-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IShellFolder
    {
        [PreserveSig] int ParseDisplayName(IntPtr hwnd, IntPtr pbc, [MarshalAs(UnmanagedType.LPWStr)] string nome,
            out uint caracteresConsumidos, out IntPtr pidl, ref uint atributos);

        [PreserveSig] int EnumObjects(IntPtr hwnd, uint flags, out IntPtr enumerador);

        [PreserveSig] int BindToObject(IntPtr pidl, IntPtr pbc, ref Guid riid, out IntPtr ppv);

        [PreserveSig] int BindToStorage(IntPtr pidl, IntPtr pbc, ref Guid riid, out IntPtr ppv);

        [PreserveSig] int CompareIDs(IntPtr lParam, IntPtr pidl1, IntPtr pidl2);

        [PreserveSig] int CreateViewObject(IntPtr hwndDono, ref Guid riid, out IntPtr ppv);

        [PreserveSig] int GetAttributesOf(uint quantidade, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] IntPtr[] pidls, ref uint atributos);

        [PreserveSig] int GetUIObjectOf(IntPtr hwndDono, uint quantidade, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] IntPtr[] pidls,
            ref Guid riid, IntPtr reservado, out IntPtr ppv);

        [PreserveSig] int GetDisplayNameOf(IntPtr pidl, uint flags, IntPtr nome);

        [PreserveSig] int SetNameOf(IntPtr hwnd, IntPtr pidl, [MarshalAs(UnmanagedType.LPWStr)] string nome, uint flags, out IntPtr pidlNovo);
    }

    /// <summary>IContextMenu (000214E4-0000-0000-C000-000000000046).</summary>
    [ComImport, Guid("000214E4-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IContextMenu
    {
        [PreserveSig] int QueryContextMenu(IntPtr hmenu, uint indice, uint idPrimeiro, uint idUltimo, uint flags);

        [PreserveSig] int InvokeCommand(ref Comum.Nativo.NativoMenuShell.CMINVOKECOMMANDINFOEX info);

        [PreserveSig] int GetCommandString(UIntPtr idComando, uint tipo, IntPtr reservado, IntPtr nome, uint tamanho);
    }

    /// <summary>IContextMenu2 (000214F4-0000-0000-C000-000000000046): acrescenta HandleMenuMsg.</summary>
    [ComImport, Guid("000214F4-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IContextMenu2
    {
        [PreserveSig] int QueryContextMenu(IntPtr hmenu, uint indice, uint idPrimeiro, uint idUltimo, uint flags);

        [PreserveSig] int InvokeCommand(ref Comum.Nativo.NativoMenuShell.CMINVOKECOMMANDINFOEX info);

        [PreserveSig] int GetCommandString(UIntPtr idComando, uint tipo, IntPtr reservado, IntPtr nome, uint tamanho);

        [PreserveSig] int HandleMenuMsg(uint mensagem, IntPtr wParam, IntPtr lParam);
    }

    /// <summary>IContextMenu3 (BCFCE0A0-EC17-11D0-8D10-00A0C90F2719): acrescenta HandleMenuMsg2 (com resultado, p/ WM_MENUCHAR).</summary>
    [ComImport, Guid("BCFCE0A0-EC17-11D0-8D10-00A0C90F2719"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IContextMenu3
    {
        [PreserveSig] int QueryContextMenu(IntPtr hmenu, uint indice, uint idPrimeiro, uint idUltimo, uint flags);

        [PreserveSig] int InvokeCommand(ref Comum.Nativo.NativoMenuShell.CMINVOKECOMMANDINFOEX info);

        [PreserveSig] int GetCommandString(UIntPtr idComando, uint tipo, IntPtr reservado, IntPtr nome, uint tamanho);

        [PreserveSig] int HandleMenuMsg(uint mensagem, IntPtr wParam, IntPtr lParam);

        [PreserveSig] int HandleMenuMsg2(uint mensagem, IntPtr wParam, IntPtr lParam, out IntPtr resultado);
    }
}
