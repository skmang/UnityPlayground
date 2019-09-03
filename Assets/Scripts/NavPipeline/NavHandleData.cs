﻿using System.Collections.Generic;
using RVO;
using UnityEngine;

/// <summary>
/// 处理寻路请求的中间数据对象
/// </summary>
public class NavHandleData
{
    #region Some Const
    public const float CHECKED_DISTANCE = 1.5f;


    #endregion

    /// <summary>
    /// 中间数据的原请求对象
    /// </summary>
    public readonly NavigationRequest sourceRequest;

    /// <summary>
    /// 主体寻路对象ID（请求该寻路的主体对象）
    /// </summary>
    public readonly System.Guid entityID;

    /// <summary>
    /// 主体寻路对象（请求该寻路的主体对象）
    /// </summary>
    public NavEntity entity;

    /// <summary>
    /// 是否为组对象
    /// </summary>
    public readonly bool isGroup;

    /// <summary>
    /// 起点 请求寻路时所在的位置
    /// </summary>
    public readonly Vector3 startPosition;

    /// <summary>
    /// 终点 请求寻路的目标位置
    /// </summary>
    public readonly Vector3 destination;

    /// <summary>
    /// 寻路上一次的Tick时间记录
    /// </summary>
    public float lastTickTime;

    /// <summary>
    /// 作为组的成员时在编队中所在的位置
    /// </summary>
    public Vector3 slotPositionWhenAsChild;

    /// <summary>
    /// 寻路路径点Index
    /// </summary>
    public int nextWaypointIndex;

    /// <summary>
    /// 寻路路径点
    /// </summary>
    public List<Vector3> wayPointList;

    /// <summary>
    /// 该对象的速度 最终会根据该值进行移动
    /// </summary>
    public Vector3 realVelocity;


    private MovementRequest _movementRequest;
    private List<NavHandleData> _childEntityDataList;


    public NavHandleData(NavigationRequest req, NavEntity targetEntity)
    {
        entity = targetEntity;
        sourceRequest = req;
        entityID = targetEntity.entityID;
        _movementRequest = new MovementRequest
        {
            entityID = entityID
        };
        isGroup = targetEntity.navEntityType == ENavEntityType.Group;
        if (isGroup)
        {
            _childEntityDataList = new List<NavHandleData>();
            NavGroup group = (NavGroup)targetEntity;
            for (int i = 0; i < group.individualList.Count; i++)
            {
                _childEntityDataList.Add(new NavHandleData(req, group.individualList[i]));
                Simulator.Instance.addAgent(group.individualList[i].controlledAgent.GetCurrentPosition().ToRVOVec2()
                , 1f, 10, 2f, 4f, 1.5f, 6f, new RVO.Vector2(0, 0));
            }
        }
        destination = req.destination;
        startPosition = targetEntity.controlledAgent.GetCurrentPosition();
    }

    public virtual MovementRequest ConvertToMovementRequest()
    {
        _movementRequest.velocity = realVelocity;
        return _movementRequest;
    }

    public List<NavHandleData> GetChildNavData()
    {
        return _childEntityDataList;
    } 
    public void UpdateWayPointIndex()
    {
        if (wayPointList != null)
        {
            if (nextWaypointIndex < wayPointList.Count)
            {
                if (Vector3.Distance(
                        NavEntity.GetCurrentPosition(entityID).XZ(),
                        wayPointList[nextWaypointIndex].XZ()
                    ) < CHECKED_DISTANCE)
                {
                    nextWaypointIndex++;
                    if (nextWaypointIndex == wayPointList.Count)
                    {
                        Debug.Log("寻路路径已经走完");
                    }
                }
            }
        }
    }

    public Vector3 GetPathfindingVelocity()
    {
        if (wayPointList != null && nextWaypointIndex < wayPointList.Count)
        {
            return (wayPointList[nextWaypointIndex].XZ() - NavEntity.GetCurrentPosition(entityID).XZ()).normalized
                 * NavEntity.GetMaxSpeed(entityID);
        }
        return Vector3.zero;
    }

    public Vector3 GetChildAverageVelocity()
    {
        Vector3 v= Vector3.zero;
        for (int i = 0; i < _childEntityDataList.Count; i++)
        {
            v += _childEntityDataList[i].realVelocity;
        }
        v /= _childEntityDataList.Count;
        return v;
    }

    private bool CheckChildrenReached()
    {
        if (isGroup == false || _childEntityDataList.Count == 0)
        {
            return true;
        }
        else
        {
            bool allReached = true;
            for (int i = 0; i < _childEntityDataList.Count; i++)
            {
                if (Vector3.Distance(NavEntity.GetCurrentPosition(_childEntityDataList[i].entityID).XZ(), _childEntityDataList[i].slotPositionWhenAsChild.XZ()) >
                    CHECKED_DISTANCE)
                {
                    allReached = false;
                    break;
                }
            }

            return allReached;
        }
    }

    // todo 1.主体移动速度应该根据所有单位动态决定 等最慢的
    // todo 2.如果理想的编队位置恰好处于了障碍物中 应该怎么处理
    public bool HasReachedTarget()
    {
        return Vector3.Distance(NavEntity.GetCurrentPosition(entityID).XZ(), destination.XZ()) < CHECKED_DISTANCE  && CheckChildrenReached();
    }
}