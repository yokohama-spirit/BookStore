using BookServiceLibrary.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookServiceLibrary.Domain.Interfaces
{
    public interface IRecoService
    {
        Task PutRecommended(string id, string? text = null);
        Task PutUnrecommended(string id, string? text = null);
        Task<List<Recommended>> GetFullReco();
        Task<List<Unrecommended>> GetFullUnreco();
    }
}
