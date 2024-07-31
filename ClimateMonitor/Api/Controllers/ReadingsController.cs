using Microsoft.AspNetCore.Mvc;
using ClimateMonitor.Services;
using ClimateMonitor.Services.Models;
using System.Text.RegularExpressions;
using ClimateMonitor.Exceptions;

namespace ClimateMonitor.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class ReadingsController : ControllerBase
{
    private readonly DeviceSecretValidatorService _secretValidator;
    private readonly AlertService _alertService;

    public ReadingsController(
        DeviceSecretValidatorService secretValidator, 
        AlertService alertService)
    {
        _secretValidator = secretValidator;
        _alertService = alertService;
    }

    /// <summary>
    /// Evaluate a sensor readings from a device and return possible alerts.
    /// </summary>
    /// <remarks>
    /// The endpoint receives sensor readings (temperature, humidity) values
    /// as well as some extra metadata (firmwareVersion), evaluates the values
    /// and generate the possible alerts the values can raise.
    /// 
    /// There are old device out there, and if they get a firmwareVersion 
    /// format error they will request a firmware update to another service.
    /// </remarks>
    /// <param name="deviceSecret">A unique identifier on the device included in the header(x-device-shared-secret).</param>
    /// <param name="deviceReadingRequest">Sensor information and extra metadata from device.</param>
    [HttpPost("evaluate")]
    public ActionResult<IEnumerable<Alert>> EvaluateReading(
        [FromHeader(Name = "x-device-shared-secret")]string deviceSecret,
        [FromBody] DeviceReadingRequest deviceReadingRequest)
    {
        try
        {
            string pattern = @"^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(?:-((?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\.(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?(?:\+([0-9a-zA-Z-]+(?:\.[0-9a-zA-Z-]+)*))?$";
            Regex regex = new Regex(pattern);

            if (!regex.IsMatch(deviceReadingRequest.FirmwareVersion))
            {
                throw new VersionException();
            }

            
            if (!_secretValidator.ValidateDeviceSecret(deviceSecret))
            {
                throw new DeviceException();
            }

            return Ok(_alertService.GetAlerts(deviceReadingRequest));
        }
        catch (VersionException vEx)
        {
            var errors = new Dictionary<string, string[]>();
            var messages = new List<string>();
            messages.Add(vEx.Message);
            errors.Add("FirmwareVersion", messages.ToArray());
            return BadRequest(new ValidationProblemDetails(errors));
        }
        catch(DeviceException dEx)
        {
            return Problem(
                    detail: dEx.Message,
                    statusCode: StatusCodes.Status401Unauthorized);
        }

    }
}
