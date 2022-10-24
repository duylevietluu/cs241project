using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using Unity.Mathematics;
using System;

abstract public class AbstractPieceScript : MonoBehaviour
{
    public int col, row;
    public int oldCol, oldRow;
    public bool isWhite;
    public BoardScript board;
    public bool hasMoved, oldHasMoved; //used for castle checking
    public AbstractPieceScript pieceCaptured = null, rookCastled = null;


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // set col, row, isBlack
    public void Init(BoardScript boardInput)
    {
        board = boardInput;

        int2 colrow = board.FindColRow(transform.position);

        col = colrow.x;
        row = colrow.y;

        isWhite = (row < 4);
        hasMoved = false;
    }

    abstract public bool LegalMove(int tocol, int torow);

    abstract public bool LegalCapture(int tocol, int torow);


    public void MoveTo(int tocol, int torow)
    {
        // CASTLING - move the rook
        if (this.GetType() == typeof(KingScript) && Mathf.Abs(tocol-col)==2)
        {
            // king side
            if (tocol == 7)
            {
                rookCastled = board.FindPiece(8, row);
                //rook.col == 8
                rookCastled.MoveTo(6, rookCastled.row);
            }
            
            // queen side
            else
            {
                rookCastled = board.FindPiece(1, row);

                rookCastled.MoveTo(4, rookCastled.row);
            }

        }

        Vector3 vector3 = new Vector3(tocol - col, torow - row);
        transform.Translate(vector3, Space.World);

        // set col & row
        oldCol = col; oldRow = row;
        col = tocol; row = torow;

        // set hasMoved
        oldHasMoved = hasMoved;
        hasMoved = true;
    }


    public void Capture(int tocol, int torow, AbstractPieceScript pieceTo)
    {
        this.MoveTo(tocol, torow);

        //delete piece capture
        pieceCaptured = pieceTo;
        board.DeletePiece(pieceTo);
    }


    // undo a move or capture: only 1 time
    public void Undo()
    {
        // move to original position
        Vector3 vector3 = new Vector3(oldCol - col, oldRow - row);
        transform.Translate(vector3, Space.World);

        // undo set col & row, hasMoved
        col = oldCol; row = oldRow;
        hasMoved = oldHasMoved;

        // CASTLE - unmove the rook
        if (rookCastled != null)
        {
            rookCastled.Undo();
            rookCastled = null;
        }

        // CAPTURE - recover the piece
        if (pieceCaptured != null)
        {
            board.RecoverPiece(pieceCaptured);
            pieceCaptured = null;
        }
    }


    // Move or Capture at square tocol,torow
    // return true if successful, false otherwise
    public bool MoveOrCapture(int tocol, int torow)
    {
        // move/ capture to col row
        AbstractPieceScript pieceTo = board.FindPiece(tocol, torow);

        // capture
        if (pieceTo != null // there is a piece
            && pieceTo.isWhite != this.isWhite // opposing piece
            && this.LegalCapture(tocol, torow)) // legal capture?
        {
            this.Capture(tocol, torow, pieceTo);

            if (board.KingSafety(this.isWhite))
                return true;
            else
            {
                this.Undo();
                return false;
            }
        }

        // move
        else if (pieceTo == null // these is NO piece
                 && this.LegalMove(tocol, torow)) // legal move?
        {
            this.MoveTo(tocol, torow);

            if (board.KingSafety(this.isWhite))
                return true;
            else
            {
                this.Undo();
                return false;
            }
        }

        else
            return false;
    }


    // simulation: dont actually do Move or Capture at square tocol, torow
    // just check whether it can move or capture there
    public bool CanMoveOrCapture(int tocol, int torow)
    {
        bool canDo = MoveOrCapture(tocol, torow);

        if (canDo)
        {
            this.Undo();
            return true;
        }
        else
            return false;
    }


    // detect if there is any piece betwwen frpos and topos
    // diagonally, verticaly, or horizontally
    public bool PieceInBetween(int frcol, int frrow, int tocol, int torow)
    {
        int icol, irow;

        if (frcol == tocol) icol = 0;
        else 
            icol = (tocol - frcol) / Math.Abs(tocol - frcol);

        if (frrow == torow) irow = 0;
        else
            irow = (torow - frrow) / Math.Abs(torow - frrow);

        //Debug.Log(icol + " " + irow);

        for (int c = frcol + icol, r = frrow + irow; 
              c != tocol || r != torow; 
              c += icol, r += irow)
        {
            // Debug.Log(c + " " + r);
            if (board.HasPiece(c, r))
                return true;
        }
        return false;
    }
}
