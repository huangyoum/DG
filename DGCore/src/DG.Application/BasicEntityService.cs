﻿using ACC.Application;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using DG.EntityFramework;
using ACC.AutoMapper;
using System.Linq;
using ACC.Convert;
using Microsoft.Extensions.Logging;
using static DG.Core.Constant;
using Microsoft.EntityFrameworkCore;
using ACC.Extensions;
namespace DG.Application
{
    public class BasicEntityService : IBasicEntityService
    {
        #region Ctor
        protected ILogger _log;
        protected readonly DGDbContext _dbContext;
        public BasicEntityService(DGDbContext db, ILoggerFactory loggerFactory)
        {
            _log = loggerFactory.CreateLogger<BasicEntityService>();
            _dbContext = db;
        } 
        #endregion

        #region 添加数据

        public Result<TDto> Add<TEntity, TDto>(TEntity entity) where TEntity : BaseEntity,new() where TDto : new()
        {
            var result = new Result<TDto>();
            try
            {
                var DbSet = _dbContext.Set<TEntity>();
                var resultEntity = DbSet.Add(entity);
                var rows=_dbContext.SaveChanges();
                if (rows > 0)
                {
                    result.Data = resultEntity.Entity.MapTo<TEntity, TDto>();
                }
                else
                {
                    throw new Exception("添加失败");
                }
                
            }
            catch (Exception ex)
            {
                result.IsError = YesNo.Yes;
                result.ErrorMessage = ex.Message;
                result.ErrorCode = ErrorCode.Exception;
                _log.LogError(ex.Message,ex);
            }
            return result;
        }

        public Result<TDto> AddDto<TEntity, TDto>(TDto dto) where TEntity : BaseEntity, new() where TDto : new()
        {
            var entity = dto.MapTo<TDto, TEntity>();
            return Add<TEntity, TDto>(entity);
        }

        #endregion

        #region 查询数据

        public Result<TDto> GetByKey<TEntity, TDto>(object key) where TEntity : BaseEntity, new() where TDto : new()
        {
            var result = new Result<TDto>();
            try
            {
                var DbSet = _dbContext.Set<TEntity>();
                var resultEntity = DbSet.Find(key);
                result.Data = resultEntity.MapTo<TEntity, TDto>();
            }
            catch (Exception ex)
            {
                result.IsError = YesNo.Yes;
                result.ErrorMessage = ex.Message;
                result.ErrorCode = ErrorCode.Exception;
                _log.LogError(ex.Message, ex);
            }
            return result;
        }

        public Result<List<TDto>> GetList<TEntity, TDto>() where TEntity : BaseEntity, new() 
        {
            var result = new Result<List<TDto>>();
            try
            {
                var DbSet = _dbContext.Set<TEntity>();
                var resultEntity = DbSet.AsNoTracking().ToList();
                result.Data = resultEntity.MapTo<TEntity, TDto>();
                result.Count = result.Data.Count;
            }
            catch (Exception ex)
            {
                result.IsError = YesNo.Yes;
                result.ErrorMessage = ex.Message;
                result.ErrorCode = ErrorCode.Exception;
                _log.LogError(ex.Message, ex);
            }
            return result;
        }

        public Result<List<TDto>> GetList<TEntity, TDto>(Expression<Func<TEntity, bool>> predicate) where TEntity : BaseEntity, new()
        {
            var result = new Result<List<TDto>>();
            try
            {
                var DbSet = _dbContext.Set<TEntity>();
                var resultEntity = DbSet.Where(predicate).AsNoTracking().ToList();
                result.Data = resultEntity.MapTo<TEntity, TDto>();
                result.Count = result.Data.Count;
            }
            catch (Exception ex)
            {
                result.IsError = YesNo.Yes;
                result.ErrorMessage = ex.Message;
                result.ErrorCode = ErrorCode.Exception;
                _log.LogError(ex.Message, ex);
            }
            return result;
        }
        public Result<List<TDto>> GetPager<TEntity, TDto>(int pageIndex, int pageSize, string orderby) where TEntity : BaseEntity, new()
        {
            var result = new Result<List<TDto>>();
            try
            {
                var DbSet = _dbContext.Set<TEntity>();
                var resultEntity = DbSet.AsNoTracking().PageBy(out int count,pageIndex,pageSize, orderby).ToList();
                if(count>0)
                {
                    result.Data = resultEntity.MapTo<TEntity, TDto>();
                }
                else
                {
                    result.IsError = YesNo.Yes;
                    result.ErrorMessage = "没有查询到您需要的数据";
                    result.ErrorCode = ErrorCode.EmptyData;
                }
                result.Count = count;
            }
            catch (Exception ex)
            {
                result.IsError = YesNo.Yes;
                result.ErrorMessage = ex.Message;
                result.ErrorCode = ErrorCode.Exception;
                _log.LogError(ex.Message, ex);
            }
            return result;
        }
        #endregion

        #region 删除数据

        public ResultBasic<int> Delete<TEntity>(TEntity entity) where TEntity : BaseEntity
        {
            var result = new ResultBasic<int>();
            try
            {
                var DbSet = _dbContext.Set<TEntity>();
                DbSet.Remove(entity);
                result.Data = _dbContext.SaveChanges();  
            }
            catch (Exception ex)
            {
                result.IsError = YesNo.Yes;
                result.ErrorMessage = ex.Message;
                result.ErrorCode = ErrorCode.Exception;
                _log.LogError(ex.Message, ex);
            }
            return result;
        }
        public ResultBasic<int> Delete<TEntity>(Expression<Func<TEntity, bool>> predicate) where TEntity : BaseEntity
        {
            var result = new ResultBasic<int>();
            try
            {
                var DbSet = _dbContext.Set<TEntity>();
                var entitys = DbSet.Where(predicate);
                var count = 0;
                foreach (var entity in entitys)
                {
                    count += Delete(entity).Data;
                }
                result.Data = count;
            }
            catch (Exception ex)
            {
                result.IsError = YesNo.Yes;
                result.ErrorMessage = ex.Message;
                result.ErrorCode = ErrorCode.Exception;
                _log.LogError(ex.Message, ex);
            }
            return result;
        }
        public ResultBasic<int> DeleteByKey<TEntity>(object key) where TEntity : BaseEntity,new()
        {
            TEntity entity = new TEntity
            {
                ID = key.Trylong()
            };
            return Delete(entity);
        }
        public ResultBasic<int> SoftDelete<TEntity>(object key) where TEntity : BaseEntity,new()
        {
            var result = new ResultBasic<int>();
            try
            {
                var DbSet = _dbContext.Set<TEntity>();
                var entityRe = DbSet.Find(key);
                entityRe.IsDeleted = true;
                return Update(entityRe);
            }
            catch (Exception ex)
            {
                result.IsError = YesNo.Yes;
                result.ErrorMessage = ex.Message;
                result.ErrorCode = ErrorCode.Exception;
                _log.LogError(ex.Message, ex);
            }
            return result;
        }
        public ResultBasic<int> SoftDelete<TEntity>(object key, object deleteUserId) where TEntity : BaseEntity, new()
        {
            var result = new ResultBasic<int>();
            try
            {
                var DbSet = _dbContext.Set<TEntity>();
                var entityRe = DbSet.Find(key);
                entityRe.IsDeleted = true;
                entityRe.UpdateUserID = deleteUserId.Trylong();
                return Update(entityRe);
            }
            catch (Exception ex)
            {
                result.IsError = YesNo.Yes;
                result.ErrorMessage = ex.Message;
                result.ErrorCode = ErrorCode.Exception;
                _log.LogError(ex.Message, ex);
            }
            return result;
        } 

        #endregion

        #region 修改数据

        public ResultBasic<int> Update<TEntity>(TEntity entity) where TEntity : BaseEntity
        {
            var result = new ResultBasic<int>();
            try
            {
                var DbSet = _dbContext.Set<TEntity>();
                var entitys = DbSet.Update(entity);
                result.Data = _dbContext.SaveChanges();
            }
            catch (Exception ex)
            {
                result.IsError = YesNo.Yes;
                result.ErrorMessage = ex.Message;
                result.ErrorCode = ErrorCode.Exception;
                _log.LogError(ex.Message, ex);
            }
            return result;
        }

        public ResultBasic<int> UpdateDto<TEntity, TDto>(TDto dto) where TEntity : BaseEntity
        {
            var entity = dto.MapTo<TDto, TEntity>();
            return Update(entity);
        }

        #endregion

        #region Dispose DbContext
        /// <summary>
        /// Dispose DbContext
        /// </summary>
        public void Dispose()
        {
            //if (_dbContext != null)
            //{
            //    _dbContext.Dispose();
            //}
        }
        #endregion
    }
}
