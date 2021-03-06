﻿using ACC.Application;
using ACC.AutoMapper;
using ACC.Common;
using ACC.Safety;
using AutoMapper;
using DG.Application;
using DG.Application.Member;
using DG.Application.Member.Dtos;
using DG.Core.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace DG.Controllers.Api
{
    public class MemberController:BaseApiController
    {
        private IBasicEntityService _entityService;
        public MemberController(IBasicEntityService basicEntityService) 
        {
            _entityService = basicEntityService;
        }
        
        
        [HttpPost]
        public Result Add(AddMemberInput input)
        {
            #region 处理密码
            var key = UserHelper.GenUserSecretkey();
            var pwd = PasswordHelper.Encrypt(input.Password, key);
            input.Password = pwd;
            input.Secretkey = key;
            input.Name = CNName.GetRandomName();
            input.Mobile = CNMobile.GetRandomMobNO();
            #endregion
            var model = _entityService.AddDto<MemberEntity, AddMemberInput>(input);
            return model;
        }

        public Result GetAll()
        {
            var model = _entityService.GetList<MemberEntity, MemberOutput>();
            return model;
        }
        public Result GetPager(int pageIndex = 1, int pageSize = 10, string orderby="id desc,name asc")
        {
            var model = _entityService.GetPager<MemberEntity, MemberOutput>(pageIndex, pageSize, orderby);
            return model;
        }

        public Result Del(long id)
        {
            return _entityService.DeleteByKey<MemberEntity>(id);
        }
        public Result SoftDel(long id)
        {
            return _entityService.SoftDelete<MemberEntity>(id);
        }
    }
}
