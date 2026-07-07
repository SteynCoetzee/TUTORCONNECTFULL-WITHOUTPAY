using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using TutorConnect.API.Data;
using TutorConnect.API.Models;

namespace TutorConnect.API.Controllers
{
    // ── DTOs ────────────────────────────────────────────────────────────────────

    public class PayFastInitiateDto
    {
        public int StudentId { get; set; }
        public decimal TotalAmount { get; set; }
        public List<PayFastLineItem> Items { get; set; } = new();
    }

    public class PayFastLineItem
    {
        public string ModuleCode { get; set; } = string.Empty;
        public string ModuleName { get; set; } = string.Empty;
        public string SessionType { get; set; } = string.Empty; // "OneOnOne" or "Group"
        public decimal Price { get; set; }
    }

    // ── Controller ──────────────────────────────────────────────────────────────

    [Route("api/[controller]")]
    [ApiController]
    public class PayFastController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly AppDbContext _db;

        public PayFastController(IConfiguration config, AppDbContext db)
        {
            _config = config;
            _db = db;
        }

        // ── POST /api/PayFast/initiate ──────────────────────────────────────────
        // Called by Angular before redirecting to PayFast.
        // Creates a Pending Payment record and returns the signed form fields.
        [HttpPost("initiate")]
        [Authorize]
        public async Task<ActionResult> Initiate([FromBody] PayFastInitiateDto dto)
        {
            if (dto.Items == null || dto.Items.Count == 0)
                return BadRequest("No items provided.");

            if (dto.TotalAmount <= 0)
                return BadRequest("Payment amount is R0.00. Please ask an administrator to set module prices before enrolling.");

            var student = await _db.Users.FindAsync(dto.StudentId);
            if (student == null) return NotFound("Student not found.");

            var cfg = _config.GetSection("PayFast");
            var merchantId  = cfg["MerchantId"]!;
            var merchantKey = cfg["MerchantKey"]!;
            var passphrase  = cfg["Passphrase"]!;
            var isSandbox   = bool.Parse(cfg["IsSandbox"] ?? "false");
            var returnUrl   = cfg["ReturnUrl"]!;
            var cancelUrl   = cfg["CancelUrl"]!;
            var notifyUrl   = cfg["NotifyUrl"]!;

            // Create a Pending payment record
            var payment = new Payment
            {
                Amount             = dto.TotalAmount,
                Payment_Date       = DateTime.UtcNow,
                Payment_Status     = "Pending",
                Student_ID         = dto.StudentId,
                Module_Code        = dto.Items[0].ModuleCode,          // primary FK
                Enrollment_Items_Json = JsonSerializer.Serialize(dto.Items) // full cart
            };
            _db.Payments.Add(payment);
            await _db.SaveChangesAsync();

            var itemDesc = string.Join(", ", dto.Items.Select(i => $"{i.ModuleName} ({i.SessionType})"));
            if (itemDesc.Length > 255) itemDesc = itemDesc[..252] + "...";

            // Build PayFast form fields — ORDER MATTERS for the signature
            var fields = new Dictionary<string, string>
            {
                ["merchant_id"]   = merchantId,
                ["merchant_key"]  = merchantKey,
                ["return_url"]    = returnUrl,
                ["cancel_url"]    = cancelUrl,
                ["notify_url"]    = notifyUrl,
                ["name_first"]    = student.FirstName ?? "Student",
                ["name_last"]     = student.LastName  ?? "User",
                ["email_address"] = student.Email,
                ["m_payment_id"]  = payment.Payment_ID.ToString(),
                ["amount"]        = dto.TotalAmount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture),
                ["item_name"]     = "TutorConnect Module Enrollment",
                ["item_description"] = itemDesc,
                ["custom_int1"]   = dto.StudentId.ToString()
            };

            fields["signature"] = GenerateSignature(fields, passphrase);

            var payFastUrl = isSandbox
                ? "https://sandbox.payfast.co.za/eng/process"
                : "https://www.payfast.co.za/eng/process";

            return Ok(new { payFastUrl, formData = fields });
        }

        // ── POST /api/PayFast/admin/process/{paymentId} ────────────────────────
        // Manual fallback: processes a Pending payment without waiting for ITN.
        // Use when PayFast ITN was not delivered (e.g. tunnel was down).
        [HttpPost("admin/process/{paymentId:int}")]
        [AllowAnonymous]
        public async Task<ActionResult> AdminProcess(int paymentId)
        {
            var payment = await _db.Payments.FindAsync(paymentId);
            if (payment == null) return NotFound("Payment not found.");
            if (payment.Payment_Status == "Paid") return Ok("Already processed.");
            if (string.IsNullOrEmpty(payment.Enrollment_Items_Json))
                return BadRequest("No enrollment items stored for this payment.");

            var items = JsonSerializer.Deserialize<List<PayFastLineItem>>(payment.Enrollment_Items_Json);
            if (items == null || items.Count == 0) return BadRequest("Empty item list.");

            var studentId = payment.Student_ID;
            foreach (var item in items)
            {
                var existing = await _db.Student_Modules
                    .FirstOrDefaultAsync(sm =>
                        sm.Student_ID  == studentId &&
                        sm.Module_Code == item.ModuleCode &&
                        sm.IsActive);

                if (existing != null)
                {
                    if (item.SessionType == "OneOnOne") { existing.Sessions_Remaining_OneOnOne += 5; existing.Can_Book_OneOnOne = true; }
                    if (item.SessionType == "Group")    { existing.Sessions_Remaining_Group    += 5; existing.Can_Book_Group    = true; }
                }
                else
                {
                    _db.Student_Modules.Add(new Student_Module
                    {
                        Student_ID                  = studentId,
                        Module_Code                 = item.ModuleCode,
                        Enrollment_Date             = DateTime.UtcNow,
                        IsActive                    = true,
                        Can_Book_OneOnOne           = item.SessionType == "OneOnOne",
                        Can_Book_Group              = item.SessionType == "Group",
                        Sessions_Remaining_OneOnOne = item.SessionType == "OneOnOne" ? 5 : 0,
                        Sessions_Remaining_Group    = item.SessionType == "Group"    ? 5 : 0
                    });
                }
            }

            payment.Payment_Status = "Paid";
            await _db.SaveChangesAsync();
            return Ok($"Enrolled {items.Count} item(s) for student {studentId}.");
        }

        // ── GET /api/PayFast/redirect-success & redirect-cancel ─────────────────
        // PayFast cannot redirect to localhost — so it redirects to these public
        // tunnel endpoints, which then bounce the browser to the Angular frontend.
        [HttpGet("redirect-success")]
        [AllowAnonymous]
        public IActionResult RedirectSuccess()
        {
            var frontendUrl = _config["PayFast:FrontendUrl"] ?? "http://localhost:4200";
            return Redirect($"{frontendUrl}/dashboard/payment-result?status=success");
        }

        [HttpGet("redirect-cancel")]
        [AllowAnonymous]
        public IActionResult RedirectCancel()
        {
            var frontendUrl = _config["PayFast:FrontendUrl"] ?? "http://localhost:4200";
            return Redirect($"{frontendUrl}/dashboard/payment-result?status=cancel");
        }

        // ── POST /api/PayFast/notify ────────────────────────────────────────────
        // PayFast's Instant Transaction Notification — server-to-server.
        // Requires a PUBLICLY ACCESSIBLE URL (won't fire on localhost).
        // When deployed, PayFast will POST here after every completed/failed payment.
        [HttpPost("notify")]
        [AllowAnonymous]
        public async Task<ActionResult> Notify([FromForm] IFormCollection form)
        {
            var pfData = form.ToDictionary(k => k.Key, v => v.Value.ToString());

            // 1. Verify signature
            var passphrase  = _config["PayFast:Passphrase"]!;
            var receivedSig = pfData.GetValueOrDefault("signature", "");
            var dataForSig  = pfData.Where(k => k.Key != "signature")
                                    .ToDictionary(k => k.Key, v => v.Value);
            if (GenerateSignature(dataForSig, passphrase) != receivedSig)
                return BadRequest("Invalid signature");

            // 2. Only act on COMPLETE payments
            if (pfData.GetValueOrDefault("payment_status") != "COMPLETE")
                return Ok(); // PayFast expects 200 even for non-COMPLETE notifications

            // 3. Look up the payment record
            if (!int.TryParse(pfData.GetValueOrDefault("m_payment_id"), out var paymentId))
                return BadRequest("Missing payment ID");

            var payment = await _db.Payments.FindAsync(paymentId);
            if (payment == null) return NotFound();

            // 4. Verify amount — always parse with InvariantCulture; PayFast sends "3400.00"
            var amountStr = pfData.GetValueOrDefault("amount_gross", "");
            if (!decimal.TryParse(amountStr,
                    System.Globalization.NumberStyles.Number,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var gross))
                return BadRequest("Missing amount");
            if (Math.Round(gross, 2) != Math.Round(payment.Amount, 2))
                return BadRequest("Amount mismatch");

            // 5. Mark payment as Paid
            payment.Payment_Status    = "Paid";
            payment.Payment_Reference = pfData.GetValueOrDefault("pf_payment_id", "");

            // 6. Process enrollments from the stored JSON
            var studentId = int.Parse(pfData.GetValueOrDefault("custom_int1", "0"));
            if (studentId > 0 && !string.IsNullOrEmpty(payment.Enrollment_Items_Json))
            {
                var items = JsonSerializer.Deserialize<List<PayFastLineItem>>(payment.Enrollment_Items_Json);
                if (items != null)
                {
                    foreach (var item in items)
                    {
                        var existing = await _db.Student_Modules
                            .FirstOrDefaultAsync(sm =>
                                sm.Student_ID   == studentId &&
                                sm.Module_Code  == item.ModuleCode &&
                                sm.IsActive);

                        if (existing != null)
                        {
                            // Top up sessions on existing enrollment
                            if (item.SessionType == "OneOnOne")
                            {
                                existing.Sessions_Remaining_OneOnOne += 5;
                                existing.Can_Book_OneOnOne = true;
                            }
                            if (item.SessionType == "Group")
                            {
                                existing.Sessions_Remaining_Group += 5;
                                existing.Can_Book_Group = true;
                            }
                        }
                        else
                        {
                            _db.Student_Modules.Add(new Student_Module
                            {
                                Student_ID               = studentId,
                                Module_Code              = item.ModuleCode,
                                Enrollment_Date          = DateTime.UtcNow,
                                IsActive                 = true,
                                Can_Book_OneOnOne        = item.SessionType == "OneOnOne",
                                Can_Book_Group           = item.SessionType == "Group",
                                Sessions_Remaining_OneOnOne = item.SessionType == "OneOnOne" ? 5 : 0,
                                Sessions_Remaining_Group    = item.SessionType == "Group"    ? 5 : 0
                            });
                        }
                    }
                }
            }

            await _db.SaveChangesAsync();
            return Ok(); // PayFast requires 200 OK
        }

        // ── PayFast MD5 signature ───────────────────────────────────────────────
        private static string BuildParamString(Dictionary<string, string> data, string passphrase)
        {
            var parts = data
                .Where(kv => !string.IsNullOrEmpty(kv.Value?.Trim()))
                .Select(kv => $"{kv.Key}={PhpUrlEncode(kv.Value.Trim())}");

            var paramString = string.Join("&", parts);

            var pp = passphrase?.Trim() ?? "";
            if (!string.IsNullOrEmpty(pp))
                paramString += $"&passphrase={PhpUrlEncode(pp)}";

            return paramString;
        }

        private static string GenerateSignature(Dictionary<string, string> data, string passphrase)
            => GenerateSignatureFromString(BuildParamString(data, passphrase));

        private static string GenerateSignatureFromString(string paramString)
        {
            var hash = MD5.HashData(Encoding.UTF8.GetBytes(paramString));
            return string.Concat(hash.Select(b => b.ToString("x2")));
        }

        // Exact replica of PHP urlencode(): only A-Za-z0-9 - _ . pass through;
        // space → +; everything else → %XX (uppercase, UTF-8 bytes).
        // .NET's WebUtility.UrlEncode() leaves ( ) ! * unencoded — PHP does not.
        private static string PhpUrlEncode(string value)
        {
            if (string.IsNullOrEmpty(value)) return value;
            var sb = new StringBuilder(value.Length * 3);
            foreach (char c in value)
            {
                if (c == ' ')
                {
                    sb.Append('+');
                }
                else if ((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') ||
                         (c >= '0' && c <= '9') || c == '-' || c == '_' || c == '.')
                {
                    sb.Append(c);
                }
                else
                {
                    foreach (var b in Encoding.UTF8.GetBytes(c.ToString()))
                        sb.Append('%').Append(b.ToString("X2"));
                }
            }
            return sb.ToString();
        }
    }
}
