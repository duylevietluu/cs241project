using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KingScript : AbstractPieceScript
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
        // castle King side
        if (!hasMoved && row == torow && tocol == 7)
        {
            AbstractPieceScript
                col6 = board.FindPiece(6, row),
                col7 = board.FindPiece(7, row),
                col8 = board.FindPiece(8, row);

            if (col6 == null && col7 == null && col8 != null
                && col8.GetType() == typeof(RookScript) && !col8.hasMoved
                && !board.KingSquareThreat(5, row, this.isWhite)
                && !board.KingSquareThreat(6, row, this.isWhite)
                && !board.KingSquareThreat(7, row, this.isWhite))
            {
                return true;
            }
        }

        // castle Queen side
        if (!hasMoved && row == torow && tocol == 3)
        {
            AbstractPieceScript
                col1 = board.FindPiece(1, row),
                col2 = board.FindPiece(2, row),
                col3 = board.FindPiece(3, row),
                col4 = board.FindPiece(4, row);

            if (col1 != null && col2 == null && col3 == null && col4 == null
                && col1.GetType() == typeof(RookScript) && !col1.hasMoved
                && !board.KingSquareThreat(5, row, this.isWhite)
                && !board.KingSquareThreat(4, row, this.isWhite)
                && !board.KingSquareThreat(3, row, this.isWhite))
            {
                return true;
            }
        }


        return LegalCapture(tocol,torow);
    }
}