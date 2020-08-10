using Repository.Repositories;
using System.Threading.Tasks;

namespace Repository.InterFace
{
    public interface IUnitOfWork
    {
        public UserInfoRepository UserInfoRepo { get; }
        public UserActivitiesRepository  UserActivitiesRepo { get; }        

        void Save();

        Task SaveAsync();
    }
}
