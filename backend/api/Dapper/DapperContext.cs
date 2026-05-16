using Dapper;
using Dapper.Contrib.Extensions;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace JanaApi.Dapper;

public class DapperContext : IDapperContext
{
    private IDbConnection connection;
    private IDbConnection Connection
    {
        get
        {
            if (connection.State == ConnectionState.Closed)
            {
                connection.Open();
            }

            return connection;
        }
    }

    public DapperContext(string connectionString)
    {
        connection = new SqliteConnection(connectionString);
    }

    public Task<IEnumerable<T>> GetDataAsync<T>(string sql, object param = null, IDbTransaction transaction = null, CommandType? commandType = null) where T : class => Connection.QueryAsync<T>(sql, param, transaction, commandType: commandType);

    public Task<IEnumerable<TReturn>> GetDataAsync<TFirst, TSecond, TReturn>(string sql, Func<TFirst, TSecond, TReturn> map, object param = null, string splitOn = "Id", IDbTransaction transaction = null, CommandType? commandType = null) where TFirst : class where TSecond : class where TReturn : class => Connection.QueryAsync(sql, map, param, transaction, splitOn: splitOn, commandType: commandType);

    public Task<IEnumerable<TReturn>> GetDataAsync<TFirst, TSecond, TThird, TReturn>(string sql, Func<TFirst, TSecond, TThird, TReturn> map, object param = null, string splitOn = "Id", IDbTransaction transaction = null, CommandType? commandType = null) where TFirst : class where TSecond : class where TThird : class where TReturn : class => Connection.QueryAsync(sql, map, param, transaction, splitOn: splitOn, commandType: commandType);

    public Task<T> GetObjectAsync<T>(string sql, object param = null, IDbTransaction transaction = null, CommandType? commandType = null) where T : class => Connection.QueryFirstOrDefaultAsync<T>(sql, param, transaction, commandType: commandType);

    public async Task<TReturn> GetObjectAsync<TFirst, TSecond, TReturn>(string sql, Func<TFirst, TSecond, TReturn> map, object param = null, string splitOn = "Id", IDbTransaction transaction = null, CommandType? commandType = null) where TFirst : class where TSecond : class where TReturn : class
    {
        var result = await Connection.QueryAsync(sql, map, param, transaction, splitOn: splitOn, commandType: commandType);
        return result.FirstOrDefault();
    }

    public async Task<TReturn> GetObjectAsync<TFirst, TSecond, TThird, TReturn>(string sql, Func<TFirst, TSecond, TThird, TReturn> map, object param = null, string splitOn = "Id", IDbTransaction transaction = null, CommandType? commandType = null) where TFirst : class where TSecond : class where TThird : class where TReturn : class
    {
        var result = await Connection.QueryAsync(sql, map, param, transaction, splitOn: splitOn, commandType: commandType);
        return result.FirstOrDefault();
    }

    public Task<T> GetSingleValueAsync<T>(string sql, object param = null, IDbTransaction transaction = null, CommandType? commandType = null) => Connection.ExecuteScalarAsync<T>(sql, param, transaction, commandType: commandType);

    public Task InsertAsync(string sql, object param = null, IDbTransaction transaction = null, CommandType? commandType = null) => Connection.ExecuteAsync(sql, param, transaction, commandType: commandType);

    public Task<int> InsertWithIdentityAsync(string sql, object param = null, IDbTransaction transaction = null, CommandType? commandType = null) => Connection.QuerySingleAsync<int>(sql, param, transaction, commandType: commandType);

    public Task<Guid> InsertWithIdentityGuidAsync(string sql, object param = null, IDbTransaction transaction = null, CommandType? commandType = null) => Connection.QuerySingleAsync<Guid>(sql, param, transaction, commandType: commandType);

    public Task UpdateAsync(string sql, object param = null, IDbTransaction transaction = null, CommandType? commandType = null) => Connection.ExecuteAsync(sql, param, transaction, commandType: commandType);

    public Task DeleteAsync(string sql, object param = null, IDbTransaction transaction = null, CommandType? commandType = null) => Connection.ExecuteAsync(sql, param, transaction, commandType: commandType);

    public Task ExecuteAsync(string sql, object param = null, IDbTransaction transaction = null, CommandType? commandType = null) => Connection.ExecuteAsync(sql, param, transaction, commandType: commandType);
    //sezione poco da dapper contrib
    public Task<int> InsertPOCOAsync<T>(T oggetto) where T : class => Connection.InsertAsync(oggetto);

    public Task<int> InsertMultiplePOCOAsync<T>(IEnumerable<T> oggetti) => Connection.InsertAsync(oggetti);

    //public Task<T> SelectPOCOAsync<T>(int id)
    //{
    //    return Connection.GetAsync<T>(id);
    //}

    //public Task<IEnumerable<T>> SelectALLPOCOAsync<T>()
    //{
    //    return Connection.GetAsync<T>();
    //}

    public Task<bool> UpdatePOCOAsync<T>(T oggetto) where T : class => Connection.UpdateAsync(oggetto);

    public Task<bool> UpdateMultiplePOCOAsync<T>(IEnumerable<T> oggetti) => Connection.UpdateAsync(oggetti);

    public Task<bool> DeletePOCOAsync<T>(T oggetto) where T : class => Connection.DeleteAsync(oggetto);

    public Task<bool> DeleteMultiplePOCOAsync<T>(IEnumerable<T> oggetti) => Connection.DeleteAsync(oggetti);

    public IDbTransaction BeginTransaction() => Connection.BeginTransaction();

    /// <summary>
    /// Close and dispose of the database connection
    /// </summary>
    public void Dispose()
    {
        if (connection.State == ConnectionState.Open)
        {
            connection.Close();
        }

        connection.Dispose();
        connection = null;
    }
}
