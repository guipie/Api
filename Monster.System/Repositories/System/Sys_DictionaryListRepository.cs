﻿/*
 *Author：jxx
 *Contact：283591387@qq.com
 *Date：2018-07-01
 * 此代码由框架生成，请勿随意更改
 */
using Monster.Sys.IRepositories;
using Monster.Core.BaseProvider;
using Monster.Core.EFDbContext;
using Monster.Core.Extensions.AutofacManager;
using Monster.Entity.DomainModels;

namespace Monster.Sys.Repositories
{
    public partial class Sys_DictionaryListRepository : RepositoryBase<Sys_DictionaryList>, ISys_DictionaryListRepository
    {
        public Sys_DictionaryListRepository(VOLContext dbContext)
        : base(dbContext)
        {

        }
        public static ISys_DictionaryListRepository Instance
        {
            get { return AutofacContainerModule.GetService<ISys_DictionaryListRepository>(); }
        }
    }
}

