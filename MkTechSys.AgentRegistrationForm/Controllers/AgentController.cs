using Microsoft.AspNetCore.Mvc;
using MkTechSys.AgentRegistrationForm.Models;
using MkTechSys.AgentRegistrationForm.Services;

namespace MkTechSys.AgentRegistrationForm.Controllers
{
    public class AgentController : Controller
    {
        private readonly IAgentRegistrationStorageService _storageService;
        private readonly ILogger<AgentController> _logger;

        public AgentController(
            IAgentRegistrationStorageService storageService,
            ILogger<AgentController> logger)
        {
            _storageService = storageService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View(new AgentRegistrationViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(AgentRegistrationViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var success = await _storageService.SaveRegistrationAsync(model);

                if (success)
                {
                    TempData["SuccessMessage"] = "Registration submitted successfully!";
                    return RedirectToAction(nameof(Register));
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Failed to save registration. Please try again.");
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing registration");
                ModelState.AddModelError(string.Empty, "An error occurred while processing your registration. Please try again later.");
                return View(model);
            }
        }
    }
}
