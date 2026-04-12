using AutoMapper;
using Infrastructure.DTOs;
using Repositories.Models;

namespace Services.Profiles;

public class TransferWorkflowProfile : Profile
{
    public TransferWorkflowProfile()
    {
        CreateMap<TransferWorkflow, TransferWorkflowDto>();
        CreateMap<TransferWorkflowDto, TransferWorkflow>();
    }
}
