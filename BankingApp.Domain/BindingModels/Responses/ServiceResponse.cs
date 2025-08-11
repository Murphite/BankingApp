
using BankingApp.Domain.Constants;

namespace BankingApp.Domain.BindingModels.Responses
{
    public class ServiceResponse
    {
        public string StatusCode { get; set; }
        public string StatusMessage { get; set; }

        public bool Successful => StatusCode == ResponseCode.SUCCESSFUL;
        public List<string>? ValidationErrors { get; set; }
        public string Trace { get; set; } = "";
    }

    public class ServiceResponse<T> : ServiceResponse
    {
        public T ResponseObject { get; set; }
        public T Data { get; set; }


        public static ServiceResponse<T> Success(T data, string message = null)
        {
            return new ServiceResponse<T>()
            {
                StatusCode = ResponseCode.SUCCESSFUL,
                StatusMessage = message ?? "Operation successful",
                ResponseObject = data
            };
        }

        public static ServiceResponse<T> Failed(string message = null, string statusCode = ResponseCode.FAILED)
        {
            return new ServiceResponse<T>()
            {
                StatusCode = statusCode,
                StatusMessage = message ?? "Operation Failed"
            };
        }
    }
    public class GenericListSearchResult<T> : ServiceResponse
    {

        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public T Result { get; set; }
        public int TotalPages { get; set; }
        public int TotalRows { get; set; }
    }

    public class Pagination<T> : List<T>
    {
        public int CurrentPage { get; private set; }
        public int TotalPages { get; private set; }
        public int PageSize { get; private set; }
        public int TotalCount { get; private set; }
        public bool HasPrevious => CurrentPage > 1;
        public bool HasNext => CurrentPage < TotalPages;

        public Pagination(List<T> items, int count, int pageNumber, int pageSize)
        {
            if (pageSize <= 0) pageSize = 10;
            TotalCount = count;
            PageSize = pageSize;
            CurrentPage = pageNumber;
            TotalPages = (int)Math.Ceiling(count / (double)pageSize);
            AddRange(items);
        }

        public static Pagination<T> ToPagedList(IEnumerable<T> source, int pageNumber, int pageSize)
        {
            var count = source.Count();
            var items = source.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
            return new Pagination<T>(items, count, pageNumber, pageSize);
        }
    }


    public class PaginatedList<T> : ServiceResponse
    {
        public PageMeta MetaData { get; set; }
        public IEnumerable<T> Data { get; set; }

        public PaginatedList()
        {
            Data = new List<T>();
        }
    }

    public class PageMeta
    {
        public int Page { get; set; }
        public int PerPage { get; set; }
        public int Total { get; set; }
        public int TotalPages { get; set; }
    }
}
