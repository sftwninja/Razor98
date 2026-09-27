// stubs of cuoapi.dll (1.3.0.0) so the ClassicUO code compiles.
// never used, ClassicUO doesn't run on 98 anyway.

using System;
using System.Runtime.InteropServices;

namespace CUO_API
{
    [StructLayout(LayoutKind.Sequential)]
    public struct PluginHeader
    {
        public int ClientVersion;
        public IntPtr HWND;
        public IntPtr OnRecv;
        public IntPtr OnSend;
        public IntPtr OnHotkeyPressed;
        public IntPtr OnMouse;
        public IntPtr OnPlayerPositionChanged;
        public IntPtr OnClientClosing;
        public IntPtr OnInitialize;
        public IntPtr OnConnected;
        public IntPtr OnDisconnected;
        public IntPtr OnFocusGained;
        public IntPtr OnFocusLost;
        public IntPtr GetUOFilePath;
        public IntPtr Recv;
        public IntPtr Send;
        public IntPtr GetPacketLength;
        public IntPtr GetPlayerPosition;
        public IntPtr CastSpell;
        public IntPtr GetStaticImage;
        public IntPtr Tick;
        public IntPtr RequestMove;
        public IntPtr SetTitle;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ArtInfo
    {
        public long Address;
        public long Size;
        public long CompressedSize;
    }

    public delegate void OnCastSpell(int idx);
    public delegate void OnClientClose();
    public delegate void OnConnected();
    public delegate void OnDisconnected();
    public delegate void OnFocusGained();
    public delegate void OnFocusLost();
    public delegate short OnGetPacketLength(int id);
    public delegate bool OnGetPlayerPosition(ref int x, ref int y, ref int z);
    public delegate void OnGetStaticImage(ushort g, ref ArtInfo art);
    public delegate string OnGetUOFilePath();
    public delegate bool OnHotkey(int key, int mod, bool pressed);
    public delegate void OnInitialize();
    public delegate void OnMouse(int button, int wheel);
    public delegate bool OnPacketSendRecv(ref byte[] data, ref int length);
    public delegate void OnSetTitle(string title);
    public delegate void OnTick();
    public delegate void OnUpdatePlayerPosition(int x, int y, int z);
    public delegate bool RequestMove(int dir, bool run);
}
