using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Bee.Data;
using Bee.Web;

namespace Bee.AuthAPI.Models
{
    [Serializable]
    public class AuthUser
    {
        #region Properties

        public Int32 Id { get; set; }
        public Int32 GroupId { get; set; }
        public String TrueName { get; set; }
        public String TrueNamePY { get; set; }
        public String MPhone { get; set; }
        public String Email { get; set; }
        public String UserName { get; set; }
        public String Password { get; set; }
        public DateTime Birthday { get; set; }
        public Int32 SexFlag { get; set; }
        public String ShenFenZheng { get; set; }
        public String RoleIds { get; set; }
        public Int32 PostTitleId { get; set; }
        public Int32 PostTypeId { get; set; }
        public Int32 DutyTitleId { get; set; }
        public Int32 ProvinceId { get; set; }
        public Int32 CityId { get; set; }
        public Int32 DistrictId { get; set; }
        public String Address { get; set; }
        public String Remark { get; set; }
        public String AvatarUrl { get; set; }
        public String SignatureUrl { get; set; }
        public String WXOpenId { get; set; }
        public String WXUnionId { get; set; }
        public Int32 Status { get; set; }
        public DateTime CreateTime { get; set; }

        #endregion

    }

    public class AuthPermission
    {
        public int Id { get; set; }
        public int ParentId { get; set; }
        public string Name { get; set; }
        public string Title { get; set; }
        public string Icon { get; set; }
        public string Res { get; set; }
        public string ExRes { get; set; }
        public int DispIndex { get; set; }
        public int ShowFlag { get; set; }
        public int DelFlag { get; set; }
        public DateTime CreateTime { get; set; }
    }
}
