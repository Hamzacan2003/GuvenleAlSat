using GuvenleAlSat.Core.Utilities.Results;
using System;
using System.Collections.Generic;
using System.Text;

namespace GuvenleAlSat.Core.Utilities.Results;

    public class Result : IResult
    {
        public bool Success { get; }
        public string Message { get; }

        public Result(bool success, string message)
        {
            Success = success;
            Message = message;
        }
        public Result(bool success) : this(success, string.Empty) { }
    }
    public class DataResult<T> : Result, IDataResult<T>
    {
        public T? Data { get; }

        public DataResult(T? data, bool success, string message) : base(success, message)
        {
            Data = data;
        }

        public DataResult(T? data, bool success) : base(success)
        {
            Data= data;
        }
    }

public class SuccessResult : Result
{
    public SuccessResult(string message) : base(true, message) { }
    public SuccessResult() : base(true) { }
}

public class ErrorResult : Result
{
    public ErrorResult(string message) : base(false, message) { }
    public ErrorResult() : base(false) { }
}

public class SuccessDataResult<T> : DataResult<T>
{
    public SuccessDataResult(T? data, string message) : base(data, true, message) { }
    public SuccessDataResult(T? data) : base(data, true) { }
}

public class ErrorDataResult<T> : DataResult<T>
{
    public ErrorDataResult(T? data, string message) : base(data, false, message) { }
    public ErrorDataResult(string message) : base(default, false, message) { }
}