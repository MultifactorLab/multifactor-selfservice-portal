using Microsoft.AspNetCore.Mvc;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultiFactorApi;

namespace MultiFactor.SelfService.Linux.Portal.Controllers;

public class Configure2FaController : ControllerBase
{
    private readonly IMultiFactorApi _selfServiceApiClient;

    public Configure2FaController(IMultiFactorApi selfServiceApiClient)
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
