/* Fake client.exe for testing injection without UO. Logs to
 * C:\TEST\INJECT.LOG whether Crypt.dll got loaded. */
#include <windows.h>
#include <stdio.h>

static FILE *logf;

static void logline(const char *msg)
{
	SYSTEMTIME t;
	GetLocalTime(&t);
	fprintf(logf, "%02d:%02d:%02d.%03d %s\n", t.wHour, t.wMinute, t.wSecond, t.wMilliseconds, msg);
	fflush(logf);
}

static LRESULT CALLBACK WndProc(HWND hwnd, UINT msg, WPARAM wp, LPARAM lp)
{
	char buf[128];
	if (msg >= WM_USER || msg == WM_CLOSE || msg == WM_DESTROY || msg == WM_COPYDATA)
	{
		sprintf(buf, "wndproc msg %04x wp %08lx lp %08lx", msg, (unsigned long)wp, (unsigned long)lp);
		logline(buf);
	}
	if (msg == WM_DESTROY)
	{
		PostQuitMessage(0);
		return 0;
	}
	return DefWindowProcA(hwnd, msg, wp, lp);
}

int WINAPI WinMain(HINSTANCE inst, HINSTANCE prev, LPSTR cmd, int show)
{
	char buf[256];
	WNDCLASSA wc;
	HWND hwnd;
	MSG m;
	int ticks = 0;
	HMODULE crypt;

	CreateDirectoryA("C:\\TEST", NULL);
	logf = fopen("C:\\TEST\\INJECT.LOG", "a");
	if (!logf)
		return 1;

	sprintf(buf, "fakeclient start, pid %lu, version %08lx", GetCurrentProcessId(), GetVersion());
	logline(buf);
	crypt = GetModuleHandleA("Crypt.dll");
	sprintf(buf, "Crypt.dll at entry: %p", (void *)crypt);
	logline(buf);

	ZeroMemory(&wc, sizeof(wc));
	wc.lpfnWndProc = WndProc;
	wc.hInstance = inst;
	wc.hCursor = LoadCursor(NULL, IDC_ARROW);
	wc.hbrBackground = (HBRUSH)GetStockObject(BLACK_BRUSH);
	wc.lpszClassName = "Ultima Online";
	RegisterClassA(&wc);
	hwnd = CreateWindowA("Ultima Online", "Ultima Online (Razor test client)", WS_OVERLAPPEDWINDOW | WS_VISIBLE,
	                     20, 20, 640, 480, NULL, NULL, inst, NULL);

	/* run ~60s */
	SetTimer(hwnd, 1, 500, NULL);
	while (GetMessageA(&m, NULL, 0, 0) > 0)
	{
		/* ours, not Crypt's 0xAA timer */
		if (m.message == WM_TIMER && m.wParam == 1)
		{
			HMODULE now = GetModuleHandleA("Crypt.dll");
			if (now != crypt)
			{
				sprintf(buf, "Crypt.dll now at %p (after %d ms)", (void *)now, ticks * 500);
				logline(buf);
				crypt = now;
			}
			if (++ticks >= 120)
				DestroyWindow(hwnd);
		}
		if (m.message >= WM_USER)
		{
			sprintf(buf, "posted msg %04x wp %08lx lp %08lx", m.message, (unsigned long)m.wParam, (unsigned long)m.lParam);
			logline(buf);
		}
		TranslateMessage(&m);
		DispatchMessageA(&m);
	}
	sprintf(buf, "message loop ended, last message %04x", m.message);
	logline(buf);
	logline(crypt ? "exit: Crypt.dll was loaded" : "exit: Crypt.dll never loaded");
	fclose(logf);
	return 0;
}
