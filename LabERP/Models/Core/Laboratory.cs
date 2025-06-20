﻿using System.ComponentModel.DataAnnotations;

namespace LabERP.Models.Core
{
    public class Laboratory
    {
        [Key]
        public string LabID { get; set; }

        [Required]
        [Display(Name = "實驗室名稱")]
        public string Name { get; set; }

        [Display(Name = "描述")]
        public string Description { get; set; }

        [Display(Name = "網站")]
        public string Website { get; set; }

        [Display(Name = "聯絡資訊")]
        public string ContactInfo { get; set; }

        public Professor Creator { get; set; }

        public List<User> Members { get; set; }

        // 建構子
        public Laboratory()
        {
            LabID = Guid.NewGuid().ToString();
            Members = new List<User>();
        }

        // 添加成員
        public bool AddMember(User member)
        {
            if (member == null)
                return false;

            // 檢查成員是否已存在
            if (Members.Exists(m => m.UserID == member.UserID))
                return false;

            Members.Add(member);
            return true;
        }

        // 移除成員
        public bool RemoveMember(string memberID)
        {
            var member = Members.Find(m => m.UserID == memberID);
            if (member == null)
                return false;

            return Members.Remove(member);
        }
    }
}