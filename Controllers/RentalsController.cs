using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CarRental.Models;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Dependency;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.IO;
using ClosedXML.Excel;


namespace CarRental.Controllers
{
    public class RentalsController : Controller
    {
        private readonly AppDbContext _context;

        public RentalsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Rentals
        public async Task<IActionResult> Index()
        {
            var role = HttpContext.Session.GetString("Role");
            var username = HttpContext.Session.GetString("Username");
            var userid = HttpContext.Session.GetString("UserId");
            

            if(role == "Admin")
            {
                return View(await _context.Rental.Include(r => r.Car).Include(r => r.UserPass).ToListAsync());
            }
            else
            {
                return View(await _context.Rental
                    .Include(r => r.Car)
                    .Include(r => r.UserPass)
                    .Where(r => r.UserId == Convert.ToInt32(userid)).ToListAsync());
            }

               
        }

        // GET: Rentals/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var rental = await _context.Rental
                .FirstOrDefaultAsync(m => m.Id == id);
            if (rental == null)
            {
                return NotFound();
            }

            return View(rental);
        }

        // GET: Rentals/Create
        public IActionResult Create()
        {
            var carAvailable = _context.Car
                .Where(c => c.Status == "Ready" || c.Status == "Tersedia")
                .ToList();

            ViewData["CarId"] = new SelectList(carAvailable, "Id","Model");

            ViewBag.PricePerDay = carAvailable.ToDictionary(c => c.Id, c => c.PricePerDay);

            return View();
        }

        // POST: Rentals/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CarId,RentDate,ReturnDate,TotalDays,TotalPrice")] Rental rental)
        {
            if (ModelState.IsValid)
            {
                var car = await _context.Car.FindAsync(rental.CarId);
                if (car == null || car.Status == "Disewa")
                {
                    ViewBag.Message = "Mobil tidak tersedia.";
                    return View(rental);
                }

                var userId = HttpContext.Session.GetString("UserId");
                if (userId == null)
                    return RedirectToAction("Login", "Account");

                rental.UserId = Convert.ToInt32(userId);
                rental.CreatedAt = DateTime.Now;
                car.Status = "Disewa";
                rental.Status = "Disewa";

                _context.Add(rental);
                _context.Update(car);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            // Jika gagal, kirim lagi data dropdown dan harga
            var cars = _context.Car.Where(c => c.Status == "Ready").ToList();
            ViewData["CarId"] = new SelectList(cars, "Id", "Merk", rental.CarId);
            ViewBag.PricePerDay = cars.ToDictionary(c => c.Id, c => c.PricePerDay);
            return View(rental);
        }

        // GET: Rentals/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var rental = await _context.Rental.FindAsync(id);
            if (rental == null)
            {
                return NotFound();
            }
            return View(rental);
        }

        // POST: Rentals/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,CarId,UserId,RentDate,ReturnDate,ActualReturnDate,TotalDays,TotalPrice,Status,CreatedAt")] Rental rental)
        {
            if (id != rental.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(rental);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RentalExists(rental.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(rental);
        }

        // GET: Rentals/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var rental = await _context.Rental
                .FirstOrDefaultAsync(m => m.Id == id);
            if (rental == null)
            {
                return NotFound();
            }

            return View(rental);
        }

        // POST: Rentals/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var rental = await _context.Rental.FindAsync(id);
            if (rental != null)
            {
                _context.Rental.Remove(rental);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool RentalExists(int id)
        {
            return _context.Rental.Any(e => e.Id == id);
        }

        [HttpGet]
        public async Task<IActionResult> ReturnCar(int? id)
        {
            if (id == null) return NotFound();

            var rental = await _context.Rental.Include(r => r.Car)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (rental == null) return NotFound();

            if(rental.Status == "Selesai")
            {
                ViewBag.Message = "Mobil ini sudah dikembalikan";
                return RedirectToAction(nameof(Index));
            }

            return View(rental);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReturnCarPost(int id, DateTime ActualReturn)
        {
            var rental = await _context.Rental
                .Include(r => r.Car)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (rental == null) return NotFound();

            var durasiAktual = (ActualReturn - rental.ReturnDate)?.TotalHours ?? 0;
            var totalHariAktual = Math.Ceiling(durasiAktual / 24);
            if (totalHariAktual < 1) totalHariAktual = 1;

            //Biaya tambahan jika telat
            var selisihHari = totalHariAktual - rental.TotalDays;
            var hargaPerhari = rental.Car.PricePerDay;
            var tambahan = (selisihHari > 0 ? Convert.ToDecimal(selisihHari) * hargaPerhari : 0);

            rental.ActualReturnDate = ActualReturn;
            rental.TotalPrice += tambahan;
            rental.Status = "Dikembalikan";
            rental.Car.Status = "Tersedia";

            await _context.SaveChangesAsync();

            TempData["Success"] = "Mobil berhasil dikembalikan";
            return RedirectToAction(nameof(Index));

        }

        [HttpGet]
        public async Task<IActionResult> Report(DateTime? startDate, DateTime? endDate)
        {
            // Ambil data rental + relasi mobil & user
            var query = _context.Rental
                .Include(r => r.Car)
                .Include(r => r.UserPass)
                .AsQueryable();

            // Filter tanggal jika diisi
            if (startDate.HasValue && endDate.HasValue)
            {
                query = query.Where(r => r.RentDate >= startDate && r.RentDate <= endDate);
            }

            var rentals = await query.OrderByDescending(r => r.RentDate).ToListAsync();

            // Total pendapatan
            var totalPendapatan = rentals.Sum(r => r.TotalPrice ?? 0);

            ViewBag.TotalPendapatan = totalPendapatan;
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");

            return View(rentals);
        }

        [HttpGet]
        public async Task<IActionResult> ExportToPdf(DateTime? startDate, DateTime? endDate)
        {
            var rentals = await _context.Rental
                .Include(r => r.Car)
                .Include(r => r.UserPass)
                .Where(r => !startDate.HasValue || (r.RentDate >= startDate && r.RentDate <= endDate))
                .OrderByDescending(r => r.RentDate)
                .ToListAsync();

            using (var stream = new MemoryStream())
            {
                Document doc = new Document(PageSize.A4, 10, 10, 10, 10);
                PdfWriter.GetInstance(doc, stream);
                doc.Open();

                var fontTitle = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16);
                var fontHeader = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10);
                var fontBody = FontFactory.GetFont(FontFactory.HELVETICA, 10);

                doc.Add(new Paragraph("Laporan Transaksi Rental Mobil", fontTitle));
                doc.Add(new Paragraph($"Periode: {startDate?.ToString("dd/MM/yyyy")} - {endDate?.ToString("dd/MM/yyyy")}\n\n", fontBody));

                PdfPTable table = new PdfPTable(6);
                table.WidthPercentage = 100;
                table.AddCell(new PdfPCell(new Phrase("No", fontHeader)));
                table.AddCell(new PdfPCell(new Phrase("Mobil", fontHeader)));
                table.AddCell(new PdfPCell(new Phrase("Penyewa", fontHeader)));
                table.AddCell(new PdfPCell(new Phrase("Tgl Sewa", fontHeader)));
                table.AddCell(new PdfPCell(new Phrase("Tgl Kembali", fontHeader)));
                table.AddCell(new PdfPCell(new Phrase("Harga Total", fontHeader)));

                int no = 1;
                foreach (var r in rentals)
                {
                    table.AddCell(new Phrase(no++.ToString(), fontBody));
                    table.AddCell(new Phrase(r.Car?.Model ?? "-", fontBody));
                    table.AddCell(new Phrase(r.UserPass?.Username ?? "-", fontBody));
                    table.AddCell(new Phrase(r.RentDate?.ToString("dd/MM/yyyy HH:mm"), fontBody));
                    table.AddCell(new Phrase(r.ReturnDate?.ToString("dd/MM/yyyy HH:mm"), fontBody));
                    table.AddCell(new Phrase($"Rp {r.TotalPrice?.ToString("N0")}", fontBody));
                }

                doc.Add(table);
                doc.Close();

                return File(stream.ToArray(), "application/pdf", $"LaporanRental_{DateTime.Now:yyyyMMdd}.pdf");
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExportToExcel(DateTime? startDate, DateTime? endDate)
        {
            var rentals = await _context.Rental
                .Include(r => r.Car)
                .Include(r => r.UserPass)
                .Where(r => !startDate.HasValue || (r.RentDate >= startDate && r.RentDate <= endDate))
                .OrderByDescending(r => r.RentDate)
                .ToListAsync();

            using (var workbook = new XLWorkbook())
            {
                var ws = workbook.Worksheets.Add("Laporan Rental");

                ws.Cell(1, 1).Value = "No";
                ws.Cell(1, 2).Value = "Mobil";
                ws.Cell(1, 3).Value = "Penyewa";
                ws.Cell(1, 4).Value = "Tanggal Sewa";
                ws.Cell(1, 5).Value = "Tanggal Kembali";
                ws.Cell(1, 6).Value = "Harga Total (Rp)";

                int row = 2;
                int no = 1;

                foreach (var r in rentals)
                {
                    ws.Cell(row, 1).Value = no++;
                    ws.Cell(row, 2).Value = r.Car?.Model;
                    ws.Cell(row, 3).Value = r.UserPass?.Username;
                    ws.Cell(row, 4).Value = r.RentDate?.ToString("dd/MM/yyyy HH:mm");
                    ws.Cell(row, 5).Value = r.ReturnDate?.ToString("dd/MM/yyyy HH:mm");
                    ws.Cell(row, 6).Value = r.TotalPrice ?? 0;
                    row++;
                }

                ws.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return File(stream.ToArray(),
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        $"LaporanRental_{DateTime.Now:yyyyMMdd}.xlsx");
                }
            }
        }


    }
}
