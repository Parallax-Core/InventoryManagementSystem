using InventoryManagementSystem.Models;
using InventoryManagementSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq; // Needed for SelectList

namespace InventoryManagementSystem.Controllers
{
    [Authorize]
    public class SuppliersController : Controller
    {
        private readonly MongoDbService _mongoDbService;

        public SuppliersController(MongoDbService mongoDbService)
        {
            _mongoDbService = mongoDbService;
        }

        // GET: Suppliers
        public async Task<IActionResult> Index(string searchString, string statusFilter)
        {
            // Start with a base filter that matches everything
            var filterBuilder = Builders<Supplier>.Filter;
            var filter = filterBuilder.Empty;

            // 1. Apply Search Filter (Name)
            if (!string.IsNullOrEmpty(searchString))
            {
                var searchRegex = new BsonRegularExpression(searchString, "i");
                filter &= filterBuilder.Regex(s => s.Name, searchRegex);
            }

            // 2. Apply Status Filter
            if (!string.IsNullOrEmpty(statusFilter))
            {
                if (statusFilter == "Active")
                {
                    filter &= filterBuilder.Eq(s => s.IsActive, true);
                }
                else if (statusFilter == "Inactive")
                {
                    filter &= filterBuilder.Eq(s => s.IsActive, false);
                }
            }
            // Fetch the filtered list
            var products = await _mongoDbService.Suppliers.Find(filter).ToListAsync();

            // --- Prepare Data for Dropdowns ---
            var suppliers = await _mongoDbService.Suppliers.Find(_ => true).ToListAsync();

            // Pass current filter values back to view to maintain state
            ViewData["CurrentFilter"] = searchString;
            ViewData["StatusFilter"] = statusFilter;

            return View(products);
        }

        // GET: Suppliers/Create
        public IActionResult Create()
        {
            // Feature 3: Initialize with one empty contact person for the form
            var model = new Supplier();
            if (model.ContactPersons == null)
            {
                model.ContactPersons = new List<ContactPerson>();
            }
            model.ContactPersons.Add(new ContactPerson());
            return View(model);
        }

        // POST: Suppliers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Supplier supplier)
        {
            // *** DATA TRIMMING LOGIC ***
            if (supplier.Name != null) supplier.Name = supplier.Name.Trim();
            if (supplier.CompanyContactNum != null) supplier.CompanyContactNum = supplier.CompanyContactNum.Trim();

            // Trim Address fields
            if (supplier.Address != null)
            {
                if (supplier.Address.StreetAddress != null) supplier.Address.StreetAddress = supplier.Address.StreetAddress.Trim();
                if (supplier.Address.PostalCode != null) supplier.Address.PostalCode = supplier.Address.PostalCode.Trim();
                // Region, Province, City, Barangay are from dropdowns, so they are likely safe, but good to trim just in case
            }

            // Trim Contact Persons
            if (supplier.ContactPersons != null)
            {
                foreach (var contact in supplier.ContactPersons)
                {
                    if (contact.Name != null) contact.Name = contact.Name.Trim();
                    if (contact.Email != null) contact.Email = contact.Email.Trim();
                    if (contact.Phone != null) contact.Phone = contact.Phone.Trim();
                }
            }
            // ***************************

            // Feature 2: Duplicate Check (Case-insensitive & Trimmed)
            var existing = await _mongoDbService.Suppliers.Find(s => s.Name.ToLower() == supplier.Name.ToLower()).FirstOrDefaultAsync();
            if (existing != null)
            {
                ModelState.AddModelError("Name", "A supplier with this name already exists.");
            }

            // Manually trigger validation for nested contact persons
            if (supplier.ContactPersons != null)
            {
                foreach (var contact in supplier.ContactPersons)
                {
                    TryValidateModel(contact);
                }
            }

            if (ModelState.IsValid)
            {
                // Feature 1: Set IsActive to true on creation
                supplier.IsActive = true;
                supplier.CreatedAt = DateTime.UtcNow;
                supplier.CreatedBy = User.Identity.Name;
                supplier.LastModifiedAt = DateTime.UtcNow;
                supplier.LastModifiedBy = User.Identity.Name;

                await _mongoDbService.Suppliers.InsertOneAsync(supplier);
                return RedirectToAction(nameof(Index));
            }
            return View(supplier);
        }

        // GET: Suppliers/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null) return NotFound();
            var supplier = await _mongoDbService.Suppliers.Find(s => s.Id == id).FirstOrDefaultAsync();
            if (supplier == null) return NotFound();

            // Feature 3: Ensure there is at least one contact person for the form
            if (supplier.ContactPersons == null || supplier.ContactPersons.Count == 0)
            {
                if (supplier.ContactPersons == null)
                {
                    supplier.ContactPersons = new List<ContactPerson>();
                }
                supplier.ContactPersons.Add(new ContactPerson());
            }
            return View(supplier);
        }

        // POST: Suppliers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, Supplier supplier)
        {
            if (id != supplier.Id) return NotFound();

            // *** DATA TRIMMING LOGIC ***
            if (supplier.Name != null) supplier.Name = supplier.Name.Trim();
            if (supplier.CompanyContactNum != null) supplier.CompanyContactNum = supplier.CompanyContactNum.Trim();

            // Trim Address fields
            if (supplier.Address != null)
            {
                if (supplier.Address.StreetAddress != null) supplier.Address.StreetAddress = supplier.Address.StreetAddress.Trim();
                if (supplier.Address.PostalCode != null) supplier.Address.PostalCode = supplier.Address.PostalCode.Trim();
            }

            // Trim Contact Persons
            if (supplier.ContactPersons != null)
            {
                foreach (var contact in supplier.ContactPersons)
                {
                    if (contact.Name != null) contact.Name = contact.Name.Trim();
                    if (contact.Email != null) contact.Email = contact.Email.Trim();
                    if (contact.Phone != null) contact.Phone = contact.Phone.Trim();
                }
            }
            // ***************************

            // Feature 2: Duplicate Check (but exclude self)
            var existing = await _mongoDbService.Suppliers.Find(s => s.Name.ToLower() == supplier.Name.ToLower() && s.Id != id).FirstOrDefaultAsync();
            if (existing != null)
            {
                ModelState.AddModelError("Name", "A supplier with this name already exists.");
            }

            // Manually trigger validation for nested contact persons
            if (supplier.ContactPersons != null)
            {
                foreach (var contact in supplier.ContactPersons)
                {
                    TryValidateModel(contact);
                }
            }

            // Fetch existing to preserve audit fields
            var supplierInDb = await _mongoDbService.Suppliers.Find(s => s.Id == id).FirstOrDefaultAsync();
            if (supplierInDb == null) return NotFound();

            if (ModelState.IsValid)
            {
                // Manually copy updated fields
                supplierInDb.Name = supplier.Name;
                supplierInDb.CompanyContactNum = supplier.CompanyContactNum;
                supplierInDb.Address = supplier.Address;
                supplierInDb.ContactPersons = supplier.ContactPersons;
                supplierInDb.IsActive = supplier.IsActive;

                // Update Audit Fields
                supplierInDb.LastModifiedAt = DateTime.UtcNow;
                supplierInDb.LastModifiedBy = User.Identity.Name;

                await _mongoDbService.Suppliers.ReplaceOneAsync(s => s.Id == id, supplierInDb);
                return RedirectToAction(nameof(Index));
            }
            return View(supplier);
        }

        // GET: Suppliers/ToggleStatus/5
        // Feature 1: Replaced Delete with ToggleStatus
        public async Task<IActionResult> ToggleStatus(string id)
        {
            if (id == null) return NotFound();
            var supplier = await _mongoDbService.Suppliers.Find(s => s.Id == id).FirstOrDefaultAsync();
            if (supplier == null) return NotFound();
            return View(supplier);
        }

        // POST: Suppliers/ToggleStatus/5
        [HttpPost, ActionName("ToggleStatus")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatusConfirmed(string id)
        {
            var supplier = await _mongoDbService.Suppliers.Find(s => s.Id == id).FirstOrDefaultAsync();
            if (supplier != null)
            {
                // Flip the IsActive boolean value
                supplier.IsActive = !supplier.IsActive;

                // Update Audit Fields
                supplier.LastModifiedAt = DateTime.UtcNow;
                supplier.LastModifiedBy = User.Identity.Name;

                await _mongoDbService.Suppliers.ReplaceOneAsync(s => s.Id == id, supplier);
            }
            return RedirectToAction(nameof(Index));
        }
    }
}