/*
 *代码由框架生成,任何更改都可能导致被代码生成器覆盖
 */
using Monster.Core.BaseProvider;
using Monster.Entity.DomainModels;

namespace Monster.Business.IServices
{
    public partial interface INewsService : IService<News>
    {
        object GetRecommendList(PageDataOptions options);
        object GetHandleOne(int key);
    }
}
