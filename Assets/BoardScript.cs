using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;

public class BoardScript : MonoBehaviour
{
    public Vector3 start;
    AbstractPieceScript[] childScripts;
    AbstractPieceScript pieceChoose = null;
    SelectboxScript selectbox;
    public Boolean turnWhite = true, aiwhite = false, aiblack = false;
    public AbstractPieceScript pawnGoneTwo = null; // for en passant

    // stockfish AI
    System.Diagnostics.Process stockfish = new System.Diagnostics.Process();

    // panel & promote
    public GameObject panel;
    public AbstractPieceScript pawnPromoting = null;
    public char choicePromote = ' ';

    // Start is called before the first frame update
    void Start()
    {
        start = new Vector3(-4, -4, 0);

        // find Selectbox
        selectbox = GetComponentsInChildren<SelectboxScript>()[0];
        selectbox.hide();


        // findPos for all pieces, using start

        childScripts = GetComponentsInChildren<AbstractPieceScript>();

        foreach (AbstractPieceScript piece in childScripts)
        {
            piece.Init(this);
        }

        // init stockfish AI
        stockfish.StartInfo.FileName = "Assets/stockfish_15_x64_avx2.exe";
        stockfish.StartInfo.UseShellExecute = false;
        stockfish.StartInfo.CreateNoWindow = true;
        stockfish.StartInfo.RedirectStandardInput = true;
        stockfish.StartInfo.RedirectStandardOutput = true;
        stockfish.Start();

        // Promotion Panel
        panel = GameObject.Find("PromotionPanel");
        panel.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        // if we are choosing things
        if (pawnPromoting != null)
        {
            Promote(pawnPromoting);
            return;
        }

        // AI turn
        if ((aiwhite && turnWhite) || (aiblack && !turnWhite))
        {
            PlayBestMove();

            // PrintBestMove();
        }

        // user turn
        else if (Input.GetMouseButtonDown(0))
        {          
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            //Debug.Log(mousePos.x + " " + mousePos.y);

            int2 colrow = FindColRow(mousePos);
            int col = colrow.x, row = colrow.y;

            // Outside
            if (col < 1 || col > 8 || row < 1 || row > 8)
                return;

            Debug.Log("Inside: " + col + " " + row);

            if (pieceChoose != null)
            {

                if (pieceChoose.MoveOrCapture(col, row)) 
                {
                    turnWhite = !turnWhite;

                    Debug.Log("piece moved");

                    // TEST CHECKMATE
                    if (CheckMated(turnWhite))
                    {
                        Debug.Log("Checkmated");
                        this.enabled = false;
                    }

                    // TEST DRAW
                    if (Draw())
                    {
                        Debug.Log("Draw");
                        this.enabled = false;
                    }
                }
                else
                    Debug.Log("illegal move");

                pieceChoose = null;
                selectbox.hide();
            }
            else
            {
                // find pieceChoose
                pieceChoose = FindPiece(col, row);

                // check if pieceChoose matches turn
                if (pieceChoose != null && pieceChoose.isWhite != turnWhite)
                    pieceChoose = null;

                if (pieceChoose != null)
                    selectbox.MoveTo(pieceChoose.col, pieceChoose.row);
            }
        }
    }


    // return the col and row of a given Vector3 pos in WorldSpace
    public int2 FindColRow(Vector3 pos)
    {
        //Vector3 diff = pos - start;
        Vector3 diff = transform.InverseTransformPoint(pos) - start;

        //Debug.Log(diff.x + " " + diff.y);

        int col = Mathf.CeilToInt(diff.x);
        int row = Mathf.CeilToInt(diff.y);

        return new int2(col, row);
    }


    // return reference to piece at row, col if available, otherwise return null
    public AbstractPieceScript FindPiece(int col, int row)
    {
        foreach (AbstractPieceScript piece in childScripts)
            if (piece.row == row && piece.col == col)
                return piece;

        return null;
    }

    public bool HasPiece(int col, int row)
    {
        return FindPiece(col, row) != null;
    }

    // find a piece of the type
    // this can find even unavailable (disabled) pieces
    public AbstractPieceScript FindPieceType<PieceType>(bool isWhite) where PieceType : AbstractPieceScript
    {
        AbstractPieceScript[] pieces = Resources.FindObjectsOfTypeAll<PieceType>();

        foreach (AbstractPieceScript piece in pieces)
            if (piece.isWhite == isWhite)
                return piece;

        return null;
    }

    // return true if king is safe; false otherwise
    public bool KingSafety(bool kingWhite)
    {
        AbstractPieceScript king = FindPieceType<KingScript>(kingWhite);

        return !KingSquareThreat(king.col, king.row, king.isWhite);
    }

    // return true if an opposing piece can capture this, not considering their king
    // generally to check if a piece can capture opposing King at a position
    // because if they can capture the enemy King, their King is not important
    public bool KingSquareThreat(int col, int row, bool pieceWhite)
    {
        foreach (AbstractPieceScript piece in childScripts)
            if (piece.isWhite != pieceWhite // opposing piece
                && piece.LegalCapture(col, row)) // can capture
            {
                return true;
            }

        return false; 
    }

    // return true if a king is checkmated; false otherwise
    public bool CheckMated(bool kingWhite)
    {
        // checked and cant get out of check
        return !KingSafety(kingWhite) && CantMoveAnything(kingWhite);
    }

    // return true if it is a draw
    public bool Draw()
    {
        // Insufficient materials
        if (childScripts.Length <= 4)
        {
            int BishopKnightWhite = 0, OtherWhite = 0;
            int BishopKnightBlack = 0, OtherBlack = 0;

            foreach (AbstractPieceScript piece in childScripts)
                if (piece.isWhite)
                {
                    if (piece.GetType() == typeof(BishopScript) || piece.GetType() == typeof(KnightScript))
                        BishopKnightWhite++;
                    else
                        OtherWhite++;
                }
                else
                {
                    if (piece.GetType() == typeof(BishopScript) || piece.GetType() == typeof(KnightScript))
                        BishopKnightBlack++;
                    else
                        OtherBlack++;
                }

            if (BishopKnightWhite <= 1 && OtherWhite <= 1
                && BishopKnightBlack <= 1 && OtherBlack <= 1)
                    return true;
        }


        // Stalemate
        if (KingSafety(turnWhite) && CantMoveAnything(turnWhite))
            return true;

        return false;
    }

    // return true if a side can move any of their piece, without leaving their king in check
    public bool CantMoveAnything(bool kingWhite)
    {
        foreach (AbstractPieceScript piece in childScripts)
            if (piece.isWhite == kingWhite) // ally piece
                for (int c = 1; c <= 8; c++)
                    for (int r = 1; r <= 8; r++)
                        if (piece.CanMoveOrCapture(c, r))
                            return false;

        return true;
    }

    // delete piece from list childScripts and destroy the gameObject attached
    public void DeletePiece(AbstractPieceScript piece)
    {
        // find the index of piece
        int i = Array.IndexOf(childScripts, piece);
        int last = childScripts.Length - 1;

        // swap the last piece with ith piece
        childScripts[i] = childScripts[last];

        // resize childScript to length-1
        Array.Resize(ref childScripts, last);

        // deactivate piece
        // Destroy(piece.gameObject);
        piece.gameObject.SetActive(false);
    }

    public void RecoverPiece(AbstractPieceScript piece)
    {
        // add the piece to the list childScripts
        Array.Resize(ref childScripts, childScripts.Length + 1);
        childScripts[childScripts.Length - 1] = piece;

        // activate piece
        piece.gameObject.SetActive(true);
    }

    public void Promote(AbstractPieceScript pawn)
    {
        if (pawnPromoting == null && !(aiwhite && turnWhite) && !(aiblack && !turnWhite))
        {
            pawnPromoting = pawn;
            panel.SetActive(true);
            return;
        }

        AbstractPieceScript copy;

        if (choicePromote == 'q')
            copy = FindPieceType<QueenScript>(pawn.isWhite);
        else if (choicePromote == 'r')
            copy = FindPieceType<RookScript>(pawn.isWhite);
        else if (choicePromote == 'b')
            copy = FindPieceType<BishopScript>(pawn.isWhite);
        else if (choicePromote == 'n')
            copy = FindPieceType<KnightScript>(pawn.isWhite);
        else
            return;
        
        AbstractPieceScript promoteTo = Instantiate(copy, this.transform);

        promoteTo.transform.localPosition = pawn.transform.localPosition;
        promoteTo.transform.localRotation = pawn.transform.localRotation;

        promoteTo.col = pawn.col; promoteTo.row = pawn.row;
        promoteTo.board = this;
        promoteTo.hasMoved = pawn.hasMoved;
        promoteTo.pieceCaptured = null; promoteTo.rookCastled = null;

        DeletePiece(pawn);
        RecoverPiece(promoteTo);

        // set to default
        panel.SetActive(false);
        pawnPromoting = null;
        choicePromote = ' ';
    }

    public void SetChoice(String piece)
    {
        choicePromote = piece[0];
    }

    string GetFenNotation()
    {
        StringBuilder ans = new StringBuilder(100);

        // get 8 rows, from 8 to 1
        for (int r = 8; r >= 1; --r)
        {
            int blank = 0;
            for (int c = 1; c <= 8; ++c)
            {
                AbstractPieceScript piece = FindPiece(c, r);
                if (piece == null)
                    blank++;
                else
                {
                    char p;
                    if (blank > 0)
                    {
                        ans.Append(blank);
                        blank = 0;
                    }
                        
                    if (piece.GetType() == typeof(PawnScript))
                        p = 'p';
                    else if (piece.GetType() == typeof(RookScript))
                        p = 'r';
                    else if (piece.GetType() == typeof(KnightScript))
                        p = 'n';
                    else if (piece.GetType() == typeof(BishopScript))
                        p = 'b';
                    else if (piece.GetType() == typeof(QueenScript))
                        p = 'q';
                    else
                        p = 'k';

                    if (piece.isWhite)
                        p = Char.ToUpper(p);

                    ans.Append(p);
                }

            }
            // closing off
            if (blank > 0)
            {
                ans.Append(blank);
                blank = 0;
            }

            if (r != 1) ans.Append('/');
            else ans.Append(' ');
        }

        // who to go
        if (turnWhite) ans.Append("w ");
        else ans.Append("b ");

        bool canCastle = false;

        // white castling avaibility
        if (!FindPieceType<KingScript>(true).hasMoved)
        {
            // kingside
            AbstractPieceScript rook = this.FindPiece(8, 1);
            if (rook != null && !rook.hasMoved)
            {
                canCastle = true;
                ans.Append('K');
            }

            // queenside
            rook = this.FindPiece(1, 1);
            if (rook != null && !rook.hasMoved)
            {
                canCastle = true;
                ans.Append('Q');
            }
        }

        // black castling avaibility
        if (!FindPieceType<KingScript>(false).hasMoved)
        {
            // kingside
            AbstractPieceScript rook = this.FindPiece(8, 8);
            if (rook != null && !rook.hasMoved)
            {
                canCastle = true;
                ans.Append('k');
            }

            // queenside
            rook = this.FindPiece(1, 8);
            if (rook != null && !rook.hasMoved)
            {
                canCastle = true;
                ans.Append('q');
            }
        }

        // ending
        if (!canCastle) ans.Append("- ");
        else ans.Append(' ');

        // en passant target square
        if (pawnGoneTwo != null)
        {
            ans.Append((char)('a' - 1 + pawnGoneTwo.col));

            if (pawnGoneTwo.isWhite)
                ans.Append(pawnGoneTwo.row - 1);
            else
                ans.Append(pawnGoneTwo.row + 1);
        }
        else
            ans.Append("-");

        // insert number of moves, this is trivial
        ans.Append(" 0 0");

        return ans.ToString();
    }

    string GetBestMove()
    {
        string setupString = "position fen " + GetFenNotation();
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

        return line;
    }
    
    // for user: print the best move for user
    void PrintBestMove()
    {
        Debug.Log(GetBestMove());
    }

    // for AI: play a best move, taken from Stockfish AI
    void PlayBestMove()
    {
        //Debug.Log("I'm thinking...");
        string bestMove = GetBestMove() + " ";

        bestMove = bestMove.Substring(9, 5);

        int fromCol = bestMove[0] - 'a' + 1, fromRow = bestMove[1] - '0';
        int toCol = bestMove[2] - 'a' + 1, toRow = bestMove[3] - '0';

        //Debug.Log(fromCol + " " + fromRow + " " + toCol + " " + toRow);

        // promoting
        choicePromote = bestMove[4];

        this.FindPiece(fromCol, fromRow).MoveOrCapture(toCol, toRow);
        turnWhite = !turnWhite;

        
        // TEST CHECKMATE
        if (CheckMated(turnWhite))
        {
            Debug.Log("Checkmated");
            this.enabled = false;
        }

        // TEST DRAW
        if (Draw())
        {
            Debug.Log("Draw");
            this.enabled = false;
        }
    }

    // flip the board
    public void FlipBoard()
    {
        transform.localScale *= -1;

        foreach (AbstractPieceScript piece in childScripts)
            piece.transform.localScale *= -1;

        selectbox.transform.localScale *= -1;

    }

    // flip white ai
    public void FlipAIWhite()
    {
        aiwhite = !aiwhite;

        if (aiwhite && turnWhite)
        {
            // eliminate user choice of piece;
            pieceChoose = null;
            selectbox.hide();
        }
    }

    // flip black ai
    public void FlipAIBlack()
    {
        aiblack = !aiblack;

        if (aiblack && !turnWhite)
        {
            // eliminate user choice of piece;
            pieceChoose = null;
            selectbox.hide();
        }
    }

    // UNO REVERSE: if human is playing with AI, flip the AI-human role
    public void UnoReverse()
    {
        if (!aiwhite && aiblack)
        {
            FlipAIBlack();
            FlipBoard();
            FlipAIWhite();
        }
        else if (!aiblack && aiwhite)
        {
            FlipAIWhite();
            FlipBoard();
            FlipAIBlack();
        }
    }
}
