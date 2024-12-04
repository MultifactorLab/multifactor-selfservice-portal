using Microsoft.AspNetCore.Mvc;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultiFactorApi;

namespace MultiFactor.SelfService.Linux.Portal.Controllers;

public class Configure2FaController : ControllerBase
{
    private readonly MultiFactorApi _selfServiceApiClient;

    public Configure2FaController(MultiFactorApi selfServiceApiClient)
    {
        _selfServiceApiClient = selfServiceApiClient;
    }
        
    [HttpGet]
    public async Task<ActionResult> Index()
    {
        var response = await _selfServiceApiClient.CreateEnrollmentRequest();
        return Redirect(response.Model.Url);
    }
}
