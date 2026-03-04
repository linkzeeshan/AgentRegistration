using MkTechSys.AgentRegistrationForm.Models;

namespace MkTechSys.AgentRegistrationForm.Services
{
    public interface IAgentRegistrationStorageService
    {
        Task<bool> SaveRegistrationAsync(AgentRegistrationViewModel model);
    }
}
