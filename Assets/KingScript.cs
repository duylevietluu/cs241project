using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;

public class KingScript : PieceScript
{
    // return if it was possible to capture to a pos,
    // given that there is an opposing piece at that pos
    public override bool LegalCapture(int tocol, int torow)
    {
        int coldiff = tocol - col;
        int rowdiff = torow - row;

        // same square
        if (coldiff == 0 && rowdiff == 0) return false;

        return (Mathf.Abs(coldiff) <= 1 && Mathf.Abs(rowdiff) <= 1);
    }

    // return if it was possible to move to a pos,
    // given that the pos is empty
    public override bool LegalMove(int tocol, int torow)
    {
        // if King is moving with 1 square 
        if (LegalCapture(tocol, torow))
            return true;

        // else: definitely castling
        if (row != torow)
            return false;
        
        // King side
        if (tocol == 7)
        {
            if (isWhite && !board.whiteKingSide) return false;
            if (!isWhite && !board.blackKingSide) return false;

            PieceScript col6 = board.FindPieceAt(6, row), col7 = board.FindPieceAt(7, row);

            return (!PieceInBetween(col, row, tocol, torow)
                && !board.KingSquareThreat(5, row, this.isWhite)
                && !board.KingSquareThreat(6, row, this.isWhite)
                && !board.KingSquareThreat(7, row, this.isWhite));
        }

        // Queen side
        if (tocol == 3)
        {
            if (isWhite && !board.whiteQueenSide) return false;
            if (!isWhite && !board.blackQueenSide) return false;

            return (!PieceInBetween(col, row, tocol, torow)
                && !board.KingSquareThreat(5, row, this.isWhite)
                && !board.KingSquareThreat(4, row, this.isWhite)
                && !board.KingSquareThreat(3, row, this.isWhite));
         
        }


        return false;
    }
}