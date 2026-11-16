using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MazeCell : MonoBehaviour
{
    [SerializeField]
    private GameObject _leftWall;

    [SerializeField]
    private GameObject _rightWall;

    [SerializeField]
    private GameObject _frontWall;

    [SerializeField]
    private GameObject _backWall;

    [SerializeField]
    private GameObject _unvisitedBlock;

    public bool IsVisited { get; private set; }

    // read-only wall state (used by GhostChase to find the open paths)
       public bool HasLeftWall  => _leftWall.activeSelf;
    public bool HasRightWall => _rightWall.activeSelf;
    public bool HasFrontWall => _frontWall.activeSelf;
    public bool HasBackWall  => _backWall.activeSelf;

    public void Visit()
    {
        IsVisited = true;
        _unvisitedBlock.SetActive(false);
    }

    public void ClearLeftWall()
    {
        _leftWall.SetActive(false);
    }

    public void ClearRightWall()
    {
        _rightWall.SetActive(false);
    }

    public void ClearFrontWall()
    {
        _frontWall.SetActive(false);
    }

    public void ClearBackWall()
    {
        _backWall.SetActive(false);
        
    }
    //rakrak
        public bool HasLeftWall()
    {
        return _leftWall.activeSelf;
    }

    public bool HasRightWall()
    {
        return _rightWall.activeSelf;
    }

    public bool HasFrontWall()
    {
        return _frontWall.activeSelf;
    }

    public bool HasBackWall()
    {
        return _backWall.activeSelf;
    }
    public void CloseLeftWall()
{
    _leftWall.SetActive(true);
}

public void CloseRightWall()
{
    _rightWall.SetActive(true);
}

public void CloseFrontWall()
{
    _frontWall.SetActive(true);
}

public void CloseBackWall()
{
    _backWall.SetActive(true);
}
}