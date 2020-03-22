﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using netcore_postgres_oauth_boiler.Models;

namespace netcore_postgres_oauth_boiler.Controllers
{
	 public class AuthController : Controller
	 {

		  private readonly DatabaseContext _context;

		  private readonly ILogger<AuthController> _logger;

		  public AuthController(ILogger<AuthController> logger, DatabaseContext context)
		  {
				_logger = logger;
				_context = context;
		  }

		  [HttpPost]
		  public async Task<IActionResult> Login([FromForm] string email, [FromForm] string password)
		  {
				Console.WriteLine("Logging in with ", email, password);

				// Loading session
				if (!HttpContext.Session.IsAvailable)
					 await HttpContext.Session.LoadAsync();

				if (HttpContext.Session.GetString("user") != null)
				{
					 return BadRequest("You are already logged in!");
				}

				BadRequestObjectResult failure = BadRequest("Wrong email/password combination!");
				var user = await _context.Users.Where(c => Regex.IsMatch(c.email, email)).FirstOrDefaultAsync();

				if (user==null)
				{
					 return failure;
				}

				if (!BCrypt.Net.BCrypt.Verify(password,user.password))
				{
					 return failure;
				}

				HttpContext.Session.SetString("user", user.id);

				return Redirect("/");
		  }

		  [HttpPost]
		  public async Task<IActionResult> Register([FromForm] string email, [FromForm] string password)
		  {
				// Loading session
				if (!HttpContext.Session.IsAvailable)
					 await HttpContext.Session.LoadAsync();

				if (HttpContext.Session.GetString("user")!=null)
				{
					 return BadRequest("You are already logged in!");
				}

				if (email==null || password==null)
				{
					 return BadRequest("Missing username or password!");
				}

				// Checking for duplicates
				var count = await _context.Users.Where(c => Regex.IsMatch(c.email, email)).CountAsync();
				if (count != 0)
				{
					 return BadRequest("This email is already taken!");
				}

				// Saving the user
				User u = new User(email, password);
				_context.Users.Add(u);
				await _context.SaveChangesAsync();

				HttpContext.Session.SetString("user", u.id);

				return Ok("You have successfully registered!");
		  }

		  [HttpGet]
		  public async Task<IActionResult> SessionTest()
		  {
				if (!HttpContext.Session.IsAvailable)
					 await HttpContext.Session.LoadAsync();
				var c = HttpContext.Session.GetString("user");

				return Ok("You are: "+c);
		  }


		  [HttpGet]
		  public async Task<IActionResult> Logout()
		  {
				// Removing session
				if (!HttpContext.Session.IsAvailable)
					 await HttpContext.Session.LoadAsync();
				HttpContext.Session.Clear();

				return Redirect("/");
		  }

		  [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		  public IActionResult Error()
		  {
				return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
		  }
	 }
}
