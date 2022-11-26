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
    public bool hasMoved; //used for castle checking
    public AbstractPieceScript pieceCaptured = null, rookCastled = null;


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


    // try to change the col, row of the piece
    // without changing sprite location
    public void TryGoTo(int tocol, int torow, AbstractPieceScript pieceTo)
    {
        // CASTLING - move the rook
        if (this.GetType() == typeof(KingScript) && Mathf.Abs(tocol-col)==2)
        {
            // king side
            if (tocol == 7)
            {
                rookCastled = board.FindPiece(8, row);
                //rook.col == 8
                rookCastled.TryGoTo(6, rookCastled.row, null);
            }
            
            // queen side
            else
            {
                rookCastled = board.FindPiece(1, row);

                rookCastled.TryGoTo(4, rookCastled.row, null);
            }
        }
        else
            rookCastled = null;

        // set col & row
        oldCol = col; oldRow = row;
        col = tocol; row = torow;

        // set pieceCaptured
        pieceCaptured = pieceTo;

        // delete pieceCaptured from the board, if any
        if (pieceCaptured != null)
            board.DeletePiece(pieceTo);
    }

    // called after TryGoTo
    // update based on col, row, oldcol, oldrow
    public void UpdateLocation()
    {
        Vector3 vector3 = new Vector3(col - oldCol, row - oldRow);

        transform.localPosition += vector3;

        //transform.Translate(vector3, Space.Self);
    }


    // undo a move or capture: only 1 time
    public void UndoTry()
    {
        // undo set col & row
        col = oldCol; row = oldRow;

        // CASTLE - unmove the rook
        if (rookCastled != null)
        {
            rookCastled.UndoTry();
            rookCastled = null;
        }

        // CAPTURE - recover the piece
        if (pieceCaptured != null)
        {
            board.RecoverPiece(pieceCaptured);
            pieceCaptured = null;
        }
    }


    // simulation: dont actually do Move or Capture at square tocol, torow
    // just check whether it can move or capture there
    public bool CanMoveOrCapture(int tocol, int torow)
    {
        // move/ capture to col row
        AbstractPieceScript pieceTo = board.FindPiece(tocol, torow);

        // capture
        if (pieceTo != null // there is a piece
            && pieceTo.isWhite != this.isWhite // opposing piece
            && this.LegalCapture(tocol, torow)) // legal capture?
        {
            this.TryGoTo(tocol, torow, pieceTo);

            bool result = board.KingSafety(this.isWhite); // does the capture leave king safe?
            
            this.UndoTry();

            return result;
        }

        // move
        else if (pieceTo == null // these is NO piece
                 && this.LegalMove(tocol, torow)) // legal move?
        {
            this.TryGoTo(tocol, torow, pieceTo);

            bool result = board.KingSafety(this.isWhite);

            this.UndoTry();

            return result;
        }

        // en passant
        else if (PossiblePassant(tocol, torow)) 
        {
            this.TryGoTo(tocol, torow, board.pawnGoneTwo);

            bool result = board.KingSafety(this.isWhite);

            this.UndoTry();

            return result;
        }
            
        else
            return false;
    }

    // ACTUALLY MOVE OR CAPTURE THING AND CHANGE SPRITE LOCATION
    // Move or Capture at square tocol,torow
    // return true if successful, false otherwise
    public bool MoveOrCapture(int tocol, int torow)
    {
        if (CanMoveOrCapture(tocol, torow))
        {
            AbstractPieceScript pieceTo = board.FindPiece(tocol, torow);

            // CHECK FOR EN PASSANT
            if (PossiblePassant(tocol, torow))
                pieceTo = board.pawnGoneTwo;

            // check if any pawn has just go 2 steps up
            if (this.GetType() == typeof(PawnScript) && Math.Abs(torow - row) == 2)
                board.pawnGoneTwo = this;
            else
                board.pawnGoneTwo = null;

            // going
            this.TryGoTo(tocol, torow, pieceTo);

            // move graphical location & set hasMoved
            this.UpdateLocation();
            hasMoved = true;

            // do this on rookCastled, if any
            if (this.rookCastled != null)
            {
                rookCastled.UpdateLocation();
                rookCastled.hasMoved = true;
            }
            
            // pawn promotion checking
            if (this.GetType() == typeof(PawnScript) && (torow == 8 || torow == 1) )
                board.Promote(this);

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


        for (int c = frcol + icol, r = frrow + irow; 
              c != tocol || r != torow; 
              c += icol, r += irow)
        {
            if (board.HasPiece(c, r))
                return true;
        }
        return false;
    }

    // return if a passant can be performed at tocol,torow
    public bool PossiblePassant(int tocol, int torow)
    {
        return this.GetType() == typeof(PawnScript) // a pawn
            && board.pawnGoneTwo != null // an opposing pawn has gone 2 steps
            && tocol == board.pawnGoneTwo.col // correct tocol
            && ((this.isWhite && torow == 6) || (!this.isWhite && torow == 3)) // correct torow
            && this.LegalCapture(tocol, torow); // legal capture
    }
}
