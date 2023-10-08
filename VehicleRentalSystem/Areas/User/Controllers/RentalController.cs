﻿using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using VehicleRentalSystem.Domain.Entities;
using VehicleRentalSystem.Domain.Constants;
using VehicleRentalSystem.Application.Interfaces.Services;
using VehicleRentalSystem.Presentation.Areas.User.ViewModels;

namespace VehicleRentalSystem.Presentation.Areas.User.Controllers;

[Authorize]
[Area("User")]
public class RentalController : Controller
{
    #region Service Injection
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IAppUserService _appUserService;
    private readonly ICustomerService _customerService;
    private readonly IVehicleService _vehicleService;
    private readonly IRentalService _rentalService;
    private readonly IBrandService _brandService;
    private readonly IOfferService _offerService;

    public RentalController(UserManager<IdentityUser> userManager,
        IAppUserService appUserService, 
        ICustomerService customerService, 
        IVehicleService vehicleService, 
        IRentalService rentalService, 
        IBrandService brandService,
        IOfferService offerService)
    {
        _appUserService = appUserService;
        _customerService = customerService;
        _vehicleService = vehicleService;
        _rentalService = rentalService;
        _brandService = brandService;
        _userManager = userManager;
        _offerService = offerService;
    }
    #endregion

    #region Razor Views
    /// <summary>
    /// Defining a view action for a user to proceed rental details and vehicle information
    /// </summary>
    public IActionResult Rental(Guid vehicleId)
    {
        var vehicle = _vehicleService.GetVehicle(vehicleId);

        if(vehicle.OfferId != null)
        {
            var offer = _offerService.GetAllOffers().FirstOrDefault(x => x.Id == vehicle.OfferId);
        }
        var brand = _brandService.GetBrand(vehicle.BrandId);
        var claimsIdentity = (ClaimsIdentity)User.Identity;
        var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
        var userId = claim.Value;
        var user = _appUserService.GetUser(userId);
        var role = _userManager.GetRolesAsync(user).Result.FirstOrDefault();

        var rent = new RentalViewModel()
        {
            VehicleId = vehicleId,
            VehicleName = $"{vehicle.Model} - {brand.Name}",
            VehicleDescription = vehicle.Description,
            VehicleFeatures = vehicle.Features,
            CustomerId = userId,
            CustomerName = user.FullName,
            CustomerAddress = user.Address,
            CustomerState = user.State,
            PhoneNumber = user.PhoneNumber,
            StartingPrice = vehicle.PricePerDay,
            HasOffer = vehicle.OfferId != null ? 
                        ((_offerService.RetrieveOffer(vehicle.OfferId).EndDate > DateTime.Now && 
                        _offerService.RetrieveOffer(vehicle.OfferId).StartDate < DateTime.Now) ? "Yes": "No") : "No",
            ActualPrice = vehicle.OfferId != null ?
                        ((_offerService.RetrieveOffer(vehicle.OfferId).EndDate > DateTime.Now &&
                        _offerService.RetrieveOffer(vehicle.OfferId).StartDate < DateTime.Now) ? 
                        (vehicle.PricePerDay - _offerService.GetAllOffers().FirstOrDefault(x => x.Id == vehicle.OfferId).Discount / 100 * vehicle.PricePerDay) : vehicle.PricePerDay) : vehicle.PricePerDay,
        };

        if(role == Constants.Admin || role == Constants.Staff)
        {
            rent.PriceForRegularAndStaffs = rent.ActualPrice - (0.25 * rent.ActualPrice);
        } 
        else if(role == Constants.Customer) 
        {
            var customer = _customerService.GetUser(user.Id);

            if (!customer.IsActive)
            {
                TempData["Delete"] = "You haven't paid the due amount of your damage request. Can't process further on.";

                return RedirectToAction("Index", "Home");
            }

            if (customer.IsRegular)
            {
                rent.PriceForRegularAndStaffs = vehicle.PricePerDay - (0.10 * vehicle.PricePerDay);
            }

            rent.CustomerCitizenshipNumber = customer.CitizenshipNumber == null ? "No citizenship found" : customer.CitizenshipNumber;
            rent.CustomerLicenseNumber = customer.LicenseNumber == null ? "No license found" : customer.LicenseNumber;
        }

        return View(rent);
    }
    #endregion

    #region API Calls
    /// <summary>
    /// Defining a post action for a user to proceed rental details
    /// </summary>
    [HttpPost]
    public IActionResult Rental(RentalViewModel model)
    {
        var vehicle = _vehicleService.GetVehicle(model.VehicleId);
        var claimsIdentity = (ClaimsIdentity)User.Identity;
        var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
        var userId = claim.Value;
        var user = _appUserService.GetUser(userId);
        var role = _userManager.GetRolesAsync(user).Result.FirstOrDefault();
        var price = 0.0;
        var days = ((model.EndDate - model.StartDate).TotalDays);

        if (role == Constants.Admin || role == Constants.Staff)
        {
            price = vehicle.PricePerDay - (0.25 * vehicle.PricePerDay);
        }
        else if (role == Constants.Customer)
        {
            var customer = _customerService.GetUser(user.Id);
            
            price = vehicle.PricePerDay;

            if (customer.IsRegular)
            {
                price = vehicle.PricePerDay - (0.10 * vehicle.PricePerDay);
            }

            if (customer.LicenseURL == null || customer.CitizenshipURL == null)
            {
                TempData["Delete"] = "Please add your citizenship and license before renting a car";

                return RedirectToAction("Documents", "Profile", new { area = "Account" });
            }
        }

        var result = new Rental()
        {
            UserId = model.CustomerId,
            VehicleId = model.VehicleId,
            StartDate = model.StartDate,
            EndDate = model.EndDate,
            TotalAmount = (float)(days * price),
        };

        vehicle.IsAvailable = false;
        _vehicleService.UpdateVehicle(vehicle);

        _rentalService.AddRental(result);
        TempData["Success"] = "Your rental request has been notified.";
        return RedirectToAction("Index", "Home");
    }
    #endregion
}