/*
 *代码由框架生成,任何更改都可能导致被代码生成器覆盖
 *Repository提供数据库操作，如果要增加数据库操作请在当前目录下Partial文件夹NewsTypeRepository编写代码
 */
using Monster.Business.IRepositories;
using Monster.Core.BaseProvider;
using Monster.Core.EFDbContext;
using Monster.Core.Extensions.AutofacManager;
using Monster.Entity.DomainModels;

namespace Monster.Business.Repositories
{
    public partial class NewsTypeRepository : RepositoryBase<NewsType> , INewsTypeRepository
    {
    public NewsTypeRepository(VOLContext dbContext)
    : base(dbContext)
    {

    }
    public static INewsTypeRepository Instance
    {
      get {  return AutofacContainerModule.GetService<INewsTypeRepository>(); } }
    }
}
