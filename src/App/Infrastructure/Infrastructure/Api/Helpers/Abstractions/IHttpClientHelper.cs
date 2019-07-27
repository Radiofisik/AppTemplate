﻿using System.Threading.Tasks;

namespace Infrastructure.Api.Helpers.Abstractions
{
    public interface IHttpClientHelper
    {
        Task<TResult> Get<TResult>(string url, params (string key, string value)[] headers);

        Task<TResult> Post<TInput, TResult>(string url, TInput data, params (string key, string value)[] headers);

        Task<TResult> Put<TInput, TResult>(string url, TInput data, params (string key, string value)[] headers);

        Task<TResult> Delete<TInput, TResult>(string url, TInput data, params (string key, string value)[] headers);
    }
}
