using AutoMapper;
using ConfigurationReaderLibrary;
using MediatR;
using ServiceA.Application.Servic;
using System.Xml.Linq;

namespace ServiceA.Application.Features.ConfigurationValues.Queries.GetByName;

public class GetByNameConfigurationValueQuery:IRequest<GetByNameConfigurationResponse>
{
    public  string Name { get; set; }
    public class GetByNameConfigurationValueQueryHandler : IRequestHandler<GetByNameConfigurationValueQuery, GetByNameConfigurationResponse>
    {
        private readonly ConfigurationReader _configurationReader;

        public GetByNameConfigurationValueQueryHandler( ConfigurationReader configurationReader)
        {
            _configurationReader = configurationReader;
        }

        public async Task<GetByNameConfigurationResponse> Handle(GetByNameConfigurationValueQuery request, CancellationToken cancellationToken)
        {
            var value = _configurationReader.GetValue<dynamic> (request.Name);
            object newValue = ValueParser.ParseString(value);
            GetByNameConfigurationResponse result=new GetByNameConfigurationResponse() { Name=request.Name,Value= newValue };
            return result;
        }
    }
}
