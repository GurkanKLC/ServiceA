using ConfigurationWebPage.Application.Features.ConfigurationSettings.Commands.Create;
using ConfigurationWebPage.Application.Features.ConfigurationSettings.Commands.Delete;
using ConfigurationWebPage.Application.Features.ConfigurationSettings.Commands.Update;
using ConfigurationWebPage.Application.Features.ConfigurationSettings.Queries.GetById;
using ConfigurationWebPage.Application.Features.ConfigurationSettings.Queries.GetListConfigurationSetting;
using ConfigurationWebPage.Models;
using ConfigurationWebPage.Services;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;

namespace ConfigurationWebPage.Controllers
{
    public class ConfigurationSettingController : BaseController
    {
        private readonly ConfigurationService _configurationService;

        public ConfigurationSettingController(ConfigurationService configurationService)
        {
            _configurationService = configurationService;
        }

        public async Task<IActionResult> IndexAsync() { 
            GetListConfigurationSettingQuery getListConfigurationSettingQuery = new() { };
            var configurationSettings = await Mediator.Send(getListConfigurationSettingQuery);
            return View(configurationSettings);
        }
        
        public IActionResult Create()
        {
            return View();
        }

        
        [HttpPost]
        public async Task<IActionResult> CreateAsync(ConfigurationSettingDto configurationSettingDto)
        {
          
            if (!ModelState.IsValid)
            {
                return View(configurationSettingDto);
            }
          
            CreateConfigurationSettingCommand createConfigurationSettingCommand=new(){configurationSettingDto=configurationSettingDto };
            await Mediator.Send(createConfigurationSettingCommand);
            return RedirectToAction("Index", "ConfigurationSetting");
        }       
        
       
        public async Task<IActionResult> Edit(GetByIdConfigurationSettingQuery getByIdConfigurationSettingQuery)
        {
            var result =await Mediator.Send(getByIdConfigurationSettingQuery);
            if (result==null)
            {
                return RedirectToAction("Index", "ConfigurationSetting");

            }
            ConfigurationSettingDto configurationSetting = new()
            {
                ApplicationName = result.ApplicationName,
                IsActive = result.IsActive,
                Name = result.Name,
                Type = result.Type,
                Value = result.Value,
            };

            ViewData["id"] = result.Id;
            return View(configurationSetting);
        }
       
        
        [HttpPost]
        public async Task<IActionResult> EditAsync(ObjectId id, ConfigurationSettingDto configurationSettingDto)
        {
            GetByIdConfigurationSettingQuery getByIdConfigurationSettingQuery=new() { Id=id};
            ConfigurationSettingResponse setting = await Mediator.Send(getByIdConfigurationSettingQuery); ;

            if (setting==null)
            {
                return RedirectToAction("Index", "ConfigurationSetting");
            }
            if (!ModelState.IsValid)
            {
                ViewData["id"] = setting.Id;
                return View(configurationSettingDto);
            }
            await Mediator.Send(new UpdateConfigurationSettingCommand() {Id=setting.Id, configurationSettingDto = configurationSettingDto });
            return RedirectToAction("Index", "ConfigurationSetting");

        }

        public async Task<IActionResult> Delete(ObjectId id)
        {
            await Mediator.Send(new DeleteConfigurationSettingCommand() { Id=id});
            return RedirectToAction("Index", "ConfigurationSetting");

        }
    }
}
