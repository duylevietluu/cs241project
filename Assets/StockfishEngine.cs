using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StockfishEngine 
{
    // stockfish AI
    System.Diagnostics.Process stockfish;

    public StockfishEngine()
    {
        stockfish = new System.Diagnostics.Process();

        // init stockfish AI
        stockfish.StartInfo.FileName = "Assets/stockfish_15_x64_avx2.exe";
        stockfish.StartInfo.UseShellExecute = false;
        stockfish.StartInfo.CreateNoWindow = true;
        stockfish.StartInfo.RedirectStandardInput = true;
        stockfish.StartInfo.RedirectStandardOutput = true;
        stockfish.Start();
    }

    public string GetBestMove(string fenNotation)
    {
        string setupString = "position fen " + fenNotation;
        stockfish.StandardInput.WriteLine(setupString);

        // Process for 0.1 seconds
        string processString = "go movetime 100";

        // Process 20 deep
        // string processString = "go depth 20";

        stockfish.StandardInput.WriteLine(processString);

        string line;

        do
        {
            line = stockfish.StandardOutput.ReadLine();
        }
        while (!(line.StartsWith("bestmove")));

        //stockfish.Close();

        return (line + " ").Substring(9, 5);
    }
}
