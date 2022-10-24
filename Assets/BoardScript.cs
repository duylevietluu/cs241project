using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;

public class BoardScript : MonoBehaviour
{
    public Vector3 start;
    AbstractPieceScript[] childScripts;
    AbstractPieceScript pieceChoose = null;
    SelectboxScript selectbox;
    public Boolean turnWhite = true;


    // Start is called before the first frame update
    void Start()
    {
        Vector3 center = transform.position;

        //Debug.Log(center.x + " " + center.y);

        start = new Vector3(center.x - 4, center.y - 4, 0);


        // find Selectbox
        selectbox = GetComponentsInChildren<SelectboxScript>()[0];
        selectbox.hide();


        // findPos for all pieces, using start

        childScripts = GetComponentsInChildren<AbstractPieceScript>();

        foreach (AbstractPieceScript piece in childScripts)
        {
            piece.Init(this);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {          
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            // Debug.Log(mousePos.x + " " + mousePos.y);

            int2 colrow = FindColRow(mousePos);
            int col = colrow.x, row = colrow.y;

            // Outside
            if (col < 1 || col > 8 || row < 1 || row > 8)
                return;

            //Debug.Log("Inside: " + col + " " + row);

            if (pieceChoose != null)
            {

                if (pieceChoose.MoveOrCapture(col, row)) 
                {
                    Debug.Log("piece moved");

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
                else
                {
                    Debug.Log("illegal move");
                }

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
                {
                    selectbox.MoveTo(pieceChoose.col, pieceChoose.row);
                }
            }
        }
    }


    // return the col and row of a given Vector3 pos in WorldSpace
    public int2 FindColRow(Vector3 pos)
    {
        Vector3 diff = pos - start;
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

    // find king of one side
    public AbstractPieceScript FindKing(bool kingWhite)
    {
        foreach (AbstractPieceScript piece in childScripts)
            if (piece.GetType() == typeof(KingScript) && piece.isWhite == kingWhite)
                return piece;

        return null;
    }

    // return true if king is safe; false otherwise
    public bool KingSafety(bool kingWhite)
    {
        AbstractPieceScript king = FindKing(kingWhite);

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

    /*
    // return true if an opposing piece can capture to this
    // without leaving their King checked
    public bool PieceSquareThreat(int col, int row, bool pieceWhite)
    {
        foreach (AbstractPieceScript piece in childScripts)
            if (piece.isWhite != pieceWhite // opposing piece
                && piece.CanMoveOrCapture(col, row)) // can capture
            {
                return true;
            }

        return false;
    }

    public bool PieceCanMoveTo(int col, int row, bool pieceWhite)
    {
        return PieceSquareThreat(col, row, !pieceWhite);
    }
    */

    // return true if a king is checkmated; false otherwise
    public bool CheckMated(bool kingWhite)
    {
        // if not check? - then not checkmated
        if (KingSafety(kingWhite))
            return false;

        return CantMoveAnything(kingWhite);
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

    public bool HasPiece(int col, int row)
    {
        return FindPiece(col, row) != null;
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

}
