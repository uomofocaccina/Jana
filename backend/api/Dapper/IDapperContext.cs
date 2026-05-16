using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace JanaApi.Dapper;

public interface IDapperContext : IDisposable
{
    Task<IEnumerable<T>> GetDataAsync<T>(string sql, object param = null, IDbTransaction transaction = null, CommandType? commandType = null)
        where T : class;

    Task<IEnumerable<TReturn>> GetDataAsync<TFirst, TSecond, TReturn>(string sql, Func<TFirst, TSecond, TReturn> map, object param = null, string splitOn = "Id", IDbTransaction transaction = null, CommandType? commandType = null)
        where TFirst : class where TSecond : class where TReturn : class;

    Task<IEnumerable<TReturn>> GetDataAsync<TFirst, TSecond, TThrid, TReturn>(string sql, Func<TFirst, TSecond, TThrid, TReturn> map, object param = null, string splitOn = "Id", IDbTransaction transaction = null, CommandType? commandType = null)
        where TFirst : class where TSecond : class where TThrid : class where TReturn : class;

    Task<T> GetObjectAsync<T>(string sql, object param = null, IDbTransaction transaction = null, CommandType? commandType = null) where T : class;

    Task<TReturn> GetObjectAsync<TFirst, TSecond, TReturn>(string sql, Func<TFirst, TSecond, TReturn> map, object param = null, string splitOn = "Id", IDbTransaction transaction = null, CommandType? commandType = null)
        where TFirst : class where TSecond : class where TReturn : class;

    Task<TReturn> GetObjectAsync<TFirst, TSecond, TThird, TReturn>(string sql, Func<TFirst, TSecond, TThird, TReturn> map, object param = null, string splitOn = "Id", IDbTransaction transaction = null, CommandType? commandType = null)
        where TFirst : class where TSecond : class where TThird : class where TReturn : class;

    Task<T> GetSingleValueAsync<T>(string sql, object param = null, IDbTransaction transaction = null, CommandType? commandType = null);

    Task InsertAsync(string sql, object param = null, IDbTransaction transaction = null, CommandType? commandType = null);

    Task<int> InsertWithIdentityAsync(string sql, object param = null, IDbTransaction transaction = null, CommandType? commandType = null);

    Task<Guid> InsertWithIdentityGuidAsync(string sql, object param = null, IDbTransaction transaction = null, CommandType? commandType = null);

    Task UpdateAsync(string sql, object param = null, IDbTransaction transaction = null, CommandType? commandType = null);

    Task DeleteAsync(string sql, object param = null, IDbTransaction transaction = null, CommandType? commandType = null);

    Task ExecuteAsync(string sql, object param = null, IDbTransaction transaction = null, CommandType? commandType = null);

    IDbTransaction BeginTransaction();

    //sezione POCO presa da dapper contrib
    Task<int> InsertPOCOAsync<T>(T classe) where T : class;

    Task<int> InsertMultiplePOCOAsync<T>(IEnumerable<T> classe);

    //Task<T> SelectPOCOAsync<T>(int id);

    //Task<IEnumerable<T>> SelectALLPOCOAsync<T>();

    Task<bool> UpdatePOCOAsync<T>(T classe) where T : class;

    Task<bool> UpdateMultiplePOCOAsync<T>(IEnumerable<T> classe);

    Task<bool> DeletePOCOAsync<T>(T classe) where T : class;

    Task<bool> DeleteMultiplePOCOAsync<T>(IEnumerable<T> classe);
}
