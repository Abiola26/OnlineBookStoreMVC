﻿using Microsoft.AspNetCore.Mvc;
using OnlineBookStoreMVC.Implementation.Interface;

namespace OnlineBookStoreMVC.Controllers
{
    public class DashboardCountController : Controller
    {
        private readonly IDashboardCountService _dashboardCountService;
        public DashboardCountController(IDashboardCountService dashboardCountService)
        {
            _dashboardCountService = dashboardCountService;
        }
        public async Task<IActionResult> Index()
        {
            var instances =
                 await _dashboardCountService.DashBoardCount();
            return View(instances);
        }
    }
}