using System.Collections.Generic;

namespace TraderApp.Interfaces
{
    public interface IRepository<T>
    {
        void Save(string filename, T data);

        T Load(string filename);
    }
}