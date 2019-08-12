using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data;
using Bee.Caching;
using Bee.Data;
using Bee;
using Bee.Util;
using Bee.Service;

namespace Bee.AuthAPI.BLL
{
    public class OnlineUserEntity
    {
        public long CurrentTick { get; set; }
        public string SessionId { get; set; }
    }

    public class OnlineUserService : BaseRunService
    {
        public OnlineUserService()
        {
            Interval = 60;// second
        }
        protected override void Run()
        {
            OnlineUserManager.Instance.CheckOnline();
            DateTime now = DateTime.Now;
            OnlineUserManager.Instance.CurrentTick = now.Ticks; 
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class OnlineUserManager
    {
        private static OnlineUserManager instance = new OnlineUserManager();

        private Dictionary<int, OnlineUserEntity> userDict = new Dictionary<int, OnlineUserEntity>();
        private long currentTick = 0;

        private List<int> onlineUserList = new List<int>();

        private OnlineUserManager()
        {
            ServiceManager.Instance.AppendTask(new OnlineUserService());
        }

        public static OnlineUserManager Instance
        {
            get
            {
                return instance;
            }
        }

        public void TickUserId(int userId)
        {
            lock (this)
            {
                if (userDict.ContainsKey(userId))
                {
                    OnlineUserEntity onlineUserEntity = userDict[userId];

                    onlineUserEntity.CurrentTick = currentTick;
                }
                else
                {
                    lock (this)
                    {
                        userDict.Add(userId, new OnlineUserEntity()
                        { 
                            CurrentTick = currentTick,
                            SessionId = HttpContextUtil.CurrentHttpContext.Session.SessionID
                        });
                    }
                }
            }
        }

        public string GetSessionId(int userId)
        {
            string result = string.Empty;

            lock (this)
            {
                if (userDict.ContainsKey(userId))
                {
                    result = userDict[userId].SessionId;
                }
            }

            return result;
        }

        public void SetSessionId(int userId, string seesionId)
        {
            TickUserId(userId);
            lock (this)
            {
                if (userDict.ContainsKey(userId))
                {
                    userDict[userId].SessionId = seesionId;
                }
            }
        }

        public void RemoveUserId(int userId)
        {
            lock (this)
            {
                userDict.Remove(userId);
            }
        }

        public List<int> OnlineUserList
        {
            get
            {
                return onlineUserList;
            }
        }

        public void CheckOnline()
        {
            lock (this)
            {
                List<int> remainList = new List<int>();
                List<int> removingList = new List<int>();
                foreach (int userId in userDict.Keys)
                {
                    if (userDict[userId].CurrentTick < currentTick)
                    {
                        removingList.Add(userId);
                    }
                    else
                    {
                        remainList.Add(userId);
                    }
                }


                foreach (int userId in removingList)
                {
                    userDict.Remove(userId);
                }

                onlineUserList = remainList;
            }
        }

        public long CurrentTick
        {
            get
            {
                return currentTick;
            }
            set
            {
                currentTick = value;
            }
        }
        
    }
}
