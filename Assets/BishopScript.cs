using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BishopScript : AbstractPieceScript
{
    // return if it was possible to capture to a pos,
    // given that there is an opposing piece at that pos
    public override bool LegalCapture(int tocol, int torow)
    {
        return LegalMove(tocol, torow);
    }

    // return if it was possible to move to a pos,
    // given that the pos is empty
    public override bool LegalMove(int tocol, int torow)
    {
        int coldiff = tocol - col;
        int rowdiff = torow - row;

        // same square
        if (coldiff == 0 && rowdiff == 0) return false;

        return Mathf.Abs(coldiff) == Mathf.Abs(rowdiff) && !PieceInBetween(col, row, tocol, torow);
    }
}
