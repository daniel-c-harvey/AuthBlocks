using NetBlocks.Models;

namespace AuthBlocksModels.ApiModels;

public class RegistrationCreatedResult : ResultBase<RegistrationCreatedResult>
{
    public string RegistrationEmail { get; set; }

    public RegistrationCreatedResult() : base()
    {
        RegistrationEmail = string.Empty;
    }
    
    public RegistrationCreatedResult(string registrationEmail) : base()
    {
        RegistrationEmail = registrationEmail;
    }
    
    public static RegistrationCreatedResult From<TOther>(TOther other, string registrationEmail)
    where TOther : ResultBase<TOther>, new()
    {
        
        var result = ResultBase<RegistrationCreatedResult>.From(other);
        result.RegistrationEmail = registrationEmail;
        return result;
    }

    public class RegistrationCreatedResultDto : ResultDtoBase<RegistrationCreatedResult, RegistrationCreatedResultDto>
    {
        public string RegistrationEmail { get; set; }

        public RegistrationCreatedResultDto() : base()
        {
            RegistrationEmail = string.Empty;   
        }
        
        public RegistrationCreatedResultDto(RegistrationCreatedResult result) : base(result)
        {
            RegistrationEmail = result.RegistrationEmail;
        }
        
        public override RegistrationCreatedResult From()
        {
            var result = base.From();
            result.RegistrationEmail = RegistrationEmail;
            return result;
        }
    }
}