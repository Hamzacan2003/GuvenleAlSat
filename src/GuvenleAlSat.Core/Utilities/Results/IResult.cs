using System;
using System.Collections.Generic;
using System.Text;

namespace GuvenleAlSat.Core.Utilities.Results;

public interface IResult
{
    bool Success {  get; }
    string Message { get; }
}

public interface IDataResult<out T> : IResult
{
    T? Data { get; }
}