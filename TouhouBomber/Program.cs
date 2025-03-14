using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace TouhouBomber;

public static partial class Program
{
    private const int ProcessWmRead = 0x0010;
    
    private static readonly Dictionary<int, (string, string, int)> Addresses = new()
    {
        [1] = ("東方紅魔郷", "Touhou 6 ~ Embodiment of Scarlet Devil", 0x006CB008),
        [2] = ("th07", "Touhou 7 ~ Perfect Cherry Blossom", 0x004BFEE0),
        [3] = ("th08", "Touhou 8 ~ Imperishable Night", 0x017D5EF8)
    };

    private enum LifeState : byte { Life, Spawning, Dead, Invincible }

    public static void Main()
    {
        int id;
        while (true)
        {
            Console.Clear();
            Console.WriteLine("Touhou Bomber");
            foreach (KeyValuePair<int, (string, string, int)> pair in Addresses)
            {
                Console.WriteLine($"{pair.Key}: {pair.Value.Item2}");
            }
            
            if (!int.TryParse(Console.ReadLine(), out id)) continue;
            if (Addresses.ContainsKey(id)) break;
        }
        
        (string processName, string name, int targetAddress) = Addresses[id];
        
        Console.WriteLine($"Please, open {name}");
        Process gameProcess = FindGame(processName);
        Console.WriteLine("Game found");
        
        ForceAutoBomb(gameProcess, targetAddress);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Process FindGame(string name)
    {
        Process[] processes;
        while (true)
        {
            processes = Process.GetProcessesByName(name);
            if (processes.Length != 0) break;
            Thread.Sleep(1000);
        }
        
        return processes[0];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ForceAutoBomb(Process process, int targetAddress)
    {
        IntPtr processHandle = OpenProcess(ProcessWmRead, false, process.Id); 
        
        var bytesRead = 0;
        var buffer = new byte[1];
        var inputs = new INPUT[1];
        var previousState = LifeState.Life;
        
        while (true)
        {
            ReadProcessMemory(checked((int)processHandle), targetAddress, buffer, buffer.Length, ref bytesRead);

            var state = (LifeState)buffer[0];
            if (state == LifeState.Dead && previousState != state)
            {
                PressBombButton(inputs);
            }
            previousState = state;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void PressBombButton(INPUT[] inputs)
    {
        var input = new INPUT { type = 1 };

        input.U.ki.wScan = ScanCodeShort.KEY_X;
        input.U.ki.dwFlags = KEYEVENTF.SCANCODE;   
        inputs[0] = input;

        SendInput(1, inputs, INPUT.Size);

        Thread.Sleep(17);
        
        input.U.ki.dwFlags = KEYEVENTF.SCANCODE | KEYEVENTF.KEYUP;
        inputs[0] = input;
        SendInput(1, inputs, INPUT.Size);
    }
    
    [LibraryImport("kernel32.dll")]
    private static partial IntPtr OpenProcess(
        int dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, int dwProcessId);

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool ReadProcessMemory(
        int hProcess, int lpBaseAddress, byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesRead);
    
    /// <summary>
    /// Declaration of external SendInput method
    /// </summary>
    [LibraryImport("user32.dll")]
    private static partial uint SendInput(
        uint nInputs, [MarshalAs(UnmanagedType.LPArray), In] INPUT[] pInputs, int cbSize);
}
