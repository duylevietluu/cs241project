using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PawnScript : PieceScript
{

    // return if it was possible to capture to a pos,
    // given that there is an opposing piece at that pos
    public override bool LegalCapture(int tocol, int torow)
    {

        int coldiff = tocol - col;
        int rowdiff = torow - row;

        if (isWhite)
            return Mathf.Abs(coldiff) == 1 && rowdiff == 1;
        else
            return Mathf.Abs(coldiff) == 1 && rowdiff == -1;

    }

    // return if it was possible to move to a pos,
    // given that the pos is empty
    public override bool LegalMove(int tocol, int torow)
    {
        int coldiff = tocol - col;
        int rowdiff = torow - row;

        // Debug.Log(coldiff + " " + rowdiff);

        // different column
        if (coldiff != 0)
            return false;

        // whitepawn
        if (isWhite)
        {
                    // starting pos at row 2
            return (row == 2 && torow == 4 && !board.HasPiece(tocol, 3)) || (rowdiff == 1);
        }
        else
        {
                    // starting pos at row 7
            return (row == 7 && torow == 5 && !board.HasPiece(tocol, 6)) || (rowdiff == -1);
        }

    }
}
