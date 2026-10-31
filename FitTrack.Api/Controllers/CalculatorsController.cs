using FitTrack.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace FitTrack.Api.Controllers;

[ApiController]
[Route("api/calculators")]
public class CalculatorsController(CalculatorService calculators) : ControllerBase
{
    [HttpPost("one-rep-max")]
    public OneRepMaxResult OneRepMax(OneRepMaxRequest request) => calculators.OneRepMax(request);

    [HttpPost("bmi")]
    public BmiResult Bmi(BmiRequest request) => calculators.Bmi(request);

    [HttpPost("tdee")]
    public TdeeResult Tdee(TdeeRequest request) => calculators.Tdee(request);

    [HttpPost("macros")]
    public MacroResult Macros(MacroRequest request) => calculators.Macros(request);

    [HttpPost("body-fat")]
    public BodyFatResult BodyFat(BodyFatRequest request) => calculators.BodyFat(request);
}
