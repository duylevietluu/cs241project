using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;

public class BoardScript : MonoBehaviour
{
    // for pieces
    public PieceScript[] clones;
    public Vector3 start;
    public List<PieceScript> allPieces;
    PieceScript pieceChoose;

    // for UI elements: highlighting the pieces
    GameObject selectBox;
    GameObject checkmateBox;
    GameObject threatBox;

    // for other chess game elements
    public bool turnWhite, aiwhite, aiblack; // for deciding turns
    public bool whiteKingSide, whiteQueenSide, blackKingSide, blackQueenSide; // for castling
    public int passantCol, passantRow; // for en passant
    public PieceScript pawnGoneTwo; // for en passant

    // panel & promote
    public GameObject panel;
    public PieceScript pawnPromoting;
    public char choicePromote;

    // stockfish
    StockfishEngine stockfishEngine;

    // past positions for undo-ing
    public LinkedList<String> pastPositions;

    // -------------------------------------------------------------------------------------------------
    // Start is called before the first frame update
    void Start()
    {
        start = new Vector3(-4, -4, 0);

        // find Boxes 
        selectBox = GameObject.Find("SelectBox");
        checkmateBox = GameObject.Find("CheckmateBox");
        threatBox = GameObject.Find("ThreatBox");

        // find Promotion Panel
        panel = GameObject.Find("PromotionPanel");
        panel.SetActive(false);

        // find Pieces and hide them
        FindClonePieces();

        // set up game element
        allPieces = new List<PieceScript>(64);
        NewBoard();

        // stockfish things
        stockfishEngine = new StockfishEngine();

        // init the stack pastPositions
        pastPositions = new LinkedList<String>();
    }

    // Update is called once per frame
    void Update()
    {
        // if we are choosing promotion pawns
        if (pawnPromoting != null)
        {
            Promote(pawnPromoting);
            return;
        }

        // AI turn
        if ((aiwhite && turnWhite) || (aiblack && !turnWhite))
            PlayBestMove();

        // user turn && user click
        else if (Input.GetMouseButtonDown(0))
            HandleUserClick();
    }

    // for Start: find Pieces used for cloning, then hide these pieces
    void FindClonePieces()
    {
        clones = GetComponentsInChildren<PieceScript>();

        foreach (PieceScript piece in clones)
        {
            piece.Init(this);
            piece.gameObject.SetActive(false);
        }
    }

    // for NewBoard: Spawn a Piece with the PieceType, isWhite, col, row provided
    public PieceScript SpawnPiece<PieceType>(bool isWhite, int col, int row) where PieceType : PieceScript
    {
        PieceScript fromClone = null, toClone;

        foreach (PieceScript piece in clones)
            if (piece.GetType() == typeof(PieceType) && piece.isWhite == isWhite)
            {
                fromClone = piece; 
                break;
            }

        toClone = Instantiate(fromClone, this.transform);

        toClone.transform.localPosition = fromClone.transform.localPosition;
        toClone.transform.localRotation = fromClone.transform.localRotation;

        toClone.Init(this);
        toClone.col = col;
        toClone.row = row;
        toClone.UpdateLocation();
        toClone.gameObject.SetActive(true);

        allPieces.Add(toClone);

        return toClone;
    }

    // for Capturing, Promotion, ClearAllPieces: delete piece from list childScripts and destroy the gameObject attached
    public void DeletePiece(PieceScript piece)
    {
        allPieces.Remove(piece);
        Destroy(piece.gameObject);
    }

    // for NewBoard: clear all piece
    public void ClearAllPieces()
    {
        foreach (PieceScript piece in allPieces)
            Destroy(piece.gameObject);
        allPieces.Clear();
    }

    // for Start and Reset: set up a new board
    public void NewBoard()
    {
        // re-enabled if this script is disabled
        this.enabled = true;

        // reset all UI elements
        selectBox.SetActive(false);
        threatBox.SetActive(false);
        checkmateBox.SetActive(false);

        // reset all game variables
        pieceChoose = null;
        turnWhite = true; aiwhite = false; aiblack = false;
        pawnGoneTwo = null;
        passantCol = 0; passantRow = 0;
        pawnPromoting = null;
        choicePromote = ' ';
        whiteKingSide = true; whiteQueenSide = true;
        blackKingSide = true; blackQueenSide = true;

        // clear all pieces from the board
        ClearAllPieces();

        // Spawn Rooks
        SpawnPiece<RookScript>(true, 1, 1);  SpawnPiece<RookScript>(true, 8, 1);
        SpawnPiece<RookScript>(false, 1, 8); SpawnPiece<RookScript>(false, 8, 8);

        // Spawn Knights
        SpawnPiece<KnightScript>(true, 2, 1); SpawnPiece<KnightScript>(true, 7, 1);
        SpawnPiece<KnightScript>(false, 2, 8); SpawnPiece<KnightScript>(false, 7, 8);

        // Spawn Bishops
        SpawnPiece<BishopScript>(true, 3, 1); SpawnPiece<BishopScript>(true, 6, 1);
        SpawnPiece<BishopScript>(false, 3, 8); SpawnPiece<BishopScript>(false, 6, 8);

        // Spawn Kings and Queens
        SpawnPiece<QueenScript>(true, 4, 1); SpawnPiece<KingScript>(true, 5, 1);
        SpawnPiece<QueenScript>(false, 4, 8); SpawnPiece<KingScript>(false, 5, 8);

        // Spawn Pawns
        for (int i = 1; i <= 8; ++i)
        {
            SpawnPiece<PawnScript>(true, i, 2);
            SpawnPiece<PawnScript>(false, i, 7);
        }
    }

    // for Undo: build a board from the FEN notation
    void NewBoardFen(string fen)
    {
        // re-enabled if this script is disabled
        this.enabled = true;

        // reset all UI elements
        selectBox.SetActive(false);
        threatBox.SetActive(false);
        checkmateBox.SetActive(false);

        // clear all pieces from the board
        ClearAllPieces();

        // reset all game variables
        pieceChoose = null; 
        aiwhite = false; aiblack = false;
        pawnPromoting = null;
        choicePromote = ' ';

        // process FEN notation
        string[] options = fen.Split(' ');

        // options[0]: pieces in the board
        string[] rows = options[0].Split('/');
        for (int r = 8, row_id = 0; r > 0; --r, ++row_id)
        {
            string row = rows[row_id];
            for (int c = 1, char_id = 0; c <= 8; ++c, ++char_id)
                if (row[char_id] == 'p')
                    SpawnPiece<PawnScript>(false, c, r);
                else if (row[char_id] == 'P')
                    SpawnPiece<PawnScript>(true, c, r);
                else if (row[char_id] == 'r')
                    SpawnPiece<RookScript>(false, c, r);
                else if (row[char_id] == 'R')
                    SpawnPiece<RookScript>(true, c, r);
                else if (row[char_id] == 'n')
                    SpawnPiece<KnightScript>(false, c, r);
                else if (row[char_id] == 'N')
                    SpawnPiece<KnightScript>(true, c, r);
                else if (row[char_id] == 'b')
                    SpawnPiece<BishopScript>(false, c, r);
                else if (row[char_id] == 'B')
                    SpawnPiece<BishopScript>(true, c, r);
                else if (row[char_id] == 'q')
                    SpawnPiece<QueenScript>(false, c, r);
                else if (row[char_id] == 'Q')
                    SpawnPiece<QueenScript>(true, c, r);
                else if (row[char_id] == 'k')
                    SpawnPiece<KingScript>(false, c, r);
                else if (row[char_id] == 'K')
                    SpawnPiece<KingScript>(true, c, r);
                else // if the char is a number
                {
                    --c; // undo the ++c
                    c += row[char_id] - '0'; 
                }
        }

        // options[1] whose turn?
        turnWhite = options[1] == "w";

        // options[2] castling availablity
        whiteKingSide = false; whiteQueenSide = false; blackKingSide = false; blackQueenSide = false;
        foreach (char ch in options[2])
            if (ch == 'K')
                whiteKingSide = true;
            else if (ch == 'k')
                blackKingSide = true;
            else if (ch == 'Q')
                whiteQueenSide = true;
            else if (ch == 'q')
                blackQueenSide = true;

        // options[3] en passant
        if (options[3] == "-")
        {
            passantCol = 0; passantRow = 0;
            pawnGoneTwo = null;
        }
        else
        {
            passantCol = options[3][0] - 'a' + 1;
            passantRow = options[3][1] - '0';

            if (passantRow == 3)
                pawnGoneTwo = FindPieceAt(passantCol, 4);
            else if (passantRow == 6)
                pawnGoneTwo = FindPieceAt(passantCol, 5);
        }

        KingUpdate();
    }

    // for handle user click: return the col and row of a given Vector3 pos in World Space
    public int2 FindColRow(Vector3 pos)
    {
        Vector3 diff = transform.InverseTransformPoint(pos) - start;

        //Debug.Log(diff.x + " " + diff.y);

        int col = Mathf.CeilToInt(diff.x);
        int row = Mathf.CeilToInt(diff.y);

        return new int2(col, row);
    }

    // for all sort of functions: return reference to piece at row, col if available, otherwise return null
    public PieceScript FindPieceAt(int col, int row)
    {
        foreach (PieceScript piece in allPieces)
            if (piece.row == row && piece.col == col)
                return piece;

        return null;
    }

    // for checking if board has piece at specific location
    public bool HasPiece(int col, int row)
    {
        return FindPieceAt(col, row) != null;
    }

    // find a piece of the type
    // this can find even unavailable (disabled) pieces
    public PieceScript FindKing(bool isWhite)
    {
        PieceScript[] pieces = GetComponentsInChildren<KingScript>();

        foreach (PieceScript piece in pieces)
            if (piece.isWhite == isWhite)
                return piece;

        return null;
    }

    // return true if king is safe; false otherwise
    public bool KingSafety(bool kingWhite)
    {
        PieceScript king = FindKing(kingWhite);
        return !KingSquareThreat(king.col, king.row, king.isWhite);
    }

    // return true if an opposing piece can capture this, not considering their king
    // generally to check if a piece can capture opposing King at a position
    // because if they can capture the enemy King, their King is not important
    public bool KingSquareThreat(int col, int row, bool pieceWhite)
    {
        foreach (PieceScript piece in allPieces)
            
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
        if (allPieces.Count <= 4)
        {
            int BishopKnightWhite = 0, OtherWhite = 0;
            int BishopKnightBlack = 0, OtherBlack = 0;

            foreach (PieceScript piece in allPieces)
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

    // for whenever a move is made: update if the king is checkmated or draw, so we can end the game
    // save this position for later use
    private void KingUpdate()
    {
        // TEST CHECKMATE
        if (CheckMated(turnWhite))
        {
            PieceScript king = FindKing(turnWhite);
            checkmateBox.SetActive(true);
            checkmateBox.transform.localPosition = king.transform.localPosition;
            Debug.Log("Checkmated");

            this.enabled = false;
        }
        else if (!KingSafety(turnWhite))
        {
            PieceScript king = FindKing(turnWhite);
            threatBox.SetActive(true);
            threatBox.transform.localPosition = king.transform.localPosition;
        }
        else
        {
            threatBox.SetActive(false);
        }
        // TEST DRAW
        if (Draw())
        {
            Debug.Log("Draw");
            this.enabled = false;
        }
    }

    // return true if a side can move any of their piece, without leaving their king in check
    public bool CantMoveAnything(bool kingWhite)
    {
        foreach (PieceScript piece in new List<PieceScript>(allPieces))
            if (piece.isWhite == kingWhite) // ally piece
                for (int c = 1; c <= 8; c++)
                    for (int r = 1; r <= 8; r++)
                        if (piece.CanMoveOrCapture(c, r))
                            return false;

        return true;
    }

    // for MoveOrCapture: called before any LEGAL actual move to check for passant and castle variables
    public void UpdatePassantAndCastle(PieceScript piece, int tocol, int torow)
    {
        // save the position
        pastPositions.AddLast(GetFenNotation());
        if (pastPositions.Count > 30) pastPositions.RemoveFirst();

        // check if any pawn has just go 2 steps up
        if (piece.GetType() == typeof(PawnScript) && Math.Abs(torow - piece.row) == 2)
            pawnGoneTwo = piece;
        else
            pawnGoneTwo = null;

        if (piece.GetType() == typeof(PawnScript) && piece.row == 2 && torow == 4)
        {
            passantCol = piece.col;
            passantRow = 3;
        }
        else if (piece.GetType() == typeof(PawnScript) && piece.row == 7 && torow == 5)
        {
            passantCol = piece.col;
            passantRow = 6;
        }
        else
        {
            // invalid col, row
            passantCol = 0;
            passantRow = 0;
        }

        // King?
        if (piece.GetType() == typeof(KingScript))
        {
            if (piece.isWhite)
            { whiteKingSide = false; whiteQueenSide = false; }
            else
            { blackKingSide = false; blackQueenSide = false; }
        }

        // Rook on King side?
        else if (piece.GetType() == typeof(RookScript) && piece.col == 8)
        {
            if (piece.isWhite && piece.row == 1)
                whiteKingSide = false;
            else if (!piece.isWhite && piece.row == 8)
                blackKingSide = false;
        }

        else if (piece.GetType() == typeof(RookScript) && piece.col == 1)
        {
            if (piece.isWhite && piece.row == 1)
                whiteQueenSide = false;
            else if (!piece.isWhite && piece.row == 8)
                blackQueenSide = false;
        }
    }

    // for promotion: 
    // if AI made the move: promote right away, using choice.
    // if user moved: show the promotion panel and pause the program
    public void Promote(PieceScript pawn)
    {
        if (pawnPromoting == null && !(aiwhite && turnWhite) && !(aiblack && !turnWhite))
        {
            pawnPromoting = pawn;
            panel.SetActive(true);
            return;
        }

        PieceScript promoteTo;

        if (choicePromote == 'q')
            promoteTo = SpawnPiece<QueenScript>(pawn.isWhite, pawn.col, pawn.row);
        else if (choicePromote == 'r')
            promoteTo = SpawnPiece<RookScript>(pawn.isWhite, pawn.col, pawn.row);
        else if (choicePromote == 'b')
            promoteTo = SpawnPiece<BishopScript>(pawn.isWhite, pawn.col, pawn.row);
        else if (choicePromote == 'n')
            promoteTo = SpawnPiece<KnightScript>(pawn.isWhite, pawn.col, pawn.row);
        else
            return;

        DeletePiece(pawn);
        allPieces.Add(promoteTo);

        // set to default
        panel.SetActive(false);
        pawnPromoting = null;
        choicePromote = ' ';
    }

    // for interacting with the panel's buttons
    public void SetPromotion(String piece)
    {
        choicePromote = piece[0];
    }

    // for user
    // handle when user click the mouse
    void HandleUserClick()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        // translate this world position to col and row
        int2 colrow = FindColRow(mousePos);
        int col = colrow.x, row = colrow.y;

        // if it is outside
        if (col < 1 || col > 8 || row < 1 || row > 8)
            return;

        //Debug.Log("Inside: " + col + " " + row);

        PieceScript pieceClicked = FindPieceAt(col, row);

        // user click the same square as pieceChoose, wishing to cancel the selection
        if (pieceClicked == pieceChoose)
        {
            pieceChoose = null;
            selectBox.SetActive(false);
        }
        // a piece is already choosen, and user click an empty or opposing square
        else if (pieceChoose != null && (pieceClicked == null || turnWhite != pieceClicked.isWhite))
        {
            if (pieceChoose.MoveOrCapture(col, row))
            {
                turnWhite = !turnWhite;
                //Debug.Log("piece moved");
                KingUpdate();
            }
            else
            {
                Debug.Log("illegal move");
            }

            pieceChoose = null;
            selectBox.SetActive(false);
        }
        // user click an ally non-empty square
        else if (pieceClicked != null && turnWhite == pieceClicked.isWhite)
        {
            // assign pieceClicked to pieceChoose
            pieceChoose = pieceClicked;

            // move the selectBox
            selectBox.SetActive(true);
            selectBox.transform.localPosition = pieceChoose.transform.localPosition;
        }
    }

    // return the FEN Notation of the current board
    string GetFenNotation()
    {
        StringBuilder ans = new StringBuilder(100);

        // get 8 rows, from 8 to 1
        for (int r = 8; r >= 1; --r)
        {
            int blank = 0;
            for (int c = 1; c <= 8; ++c)
            {
                PieceScript piece = FindPieceAt(c, r);
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
        if (whiteKingSide)
        {
            canCastle = true;
            ans.Append('K');
        }
        if (whiteQueenSide)
        {
            canCastle = true;
            ans.Append('Q');
        }

        // black castling avaibility
        if (blackKingSide)
        {
            canCastle = true;
            ans.Append('k');
        }
        if (blackQueenSide)
        {
            canCastle = true;
            ans.Append('q');
        }

        // ending
        if (!canCastle) ans.Append("- ");
        else ans.Append(' ');

        // en passant target square
        if (pawnGoneTwo != null)
        {
            ans.Append((char)('a' - 1 + passantCol));
            ans.Append(passantRow);
        }
        else
            ans.Append('-');
        

        // insert number of moves, this is trivial
        ans.Append(" 0 0");

        return ans.ToString();
    }

    // for AI: play a best move, taken from Stockfish AI
    void PlayBestMove()
    {
        //Debug.Log("I'm thinking...");
        string bestMove = stockfishEngine.GetBestMove(GetFenNotation()) ;

        int fromCol = bestMove[0] - 'a' + 1, fromRow = bestMove[1] - '0';
        int toCol = bestMove[2] - 'a' + 1, toRow = bestMove[3] - '0';

        //Debug.Log(fromCol + " " + fromRow + " " + toCol + " " + toRow);

        // promoting
        choicePromote = bestMove[4];

        this.FindPieceAt(fromCol, fromRow).MoveOrCapture(toCol, toRow);
        turnWhite = !turnWhite;

        KingUpdate();
    }

    // flip the board
    public void FlipBoard()
    {
        transform.localScale *= -1;

        foreach (PieceScript piece in clones)
            piece.transform.localScale *= -1;

        foreach (PieceScript piece in allPieces)
            piece.transform.localScale *= -1;
    }

    // flip white ai
    public void FlipAIWhite()
    {
        aiwhite = !aiwhite;

        if (aiwhite && turnWhite)
        {
            // eliminate user choice of piece;
            pieceChoose = null;
            selectBox.SetActive(false);
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
            selectBox.SetActive(false);
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

    // for Undo: pull the last position out of pastPositions, and build the board on it
    void RewindOne()
    {
        if (pastPositions.Count == 0)
        {
            Debug.Log("Out of moves to Undo!");
            return;
        }

        // pop the position out of the stack
        String last = pastPositions.Last();
        pastPositions.RemoveLast();

        // build the board
        NewBoardFen(last);
    }

    // for Undo Button:
    // undo 1 move if both users are playing
    // undo 2 moves if user are playing against AI
    // no moves if an AI is in progress on moving their pieces
    public void Undo()
    {
        if (!aiwhite && !aiblack)
            RewindOne();
        else if (!aiwhite && aiblack && turnWhite)
        {
            RewindOne();
            RewindOne();
            aiblack = true;
        }
        else if (aiwhite && !aiblack && !turnWhite)
        {
            RewindOne();
            RewindOne();
            aiwhite = true;
        }
        else
        {
            Debug.Log("You are trying to Undo when AI are thinking!");
        }
    }
}
