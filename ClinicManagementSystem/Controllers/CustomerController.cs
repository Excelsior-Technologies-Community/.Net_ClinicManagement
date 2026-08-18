using ClinicManagementSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Data;
using System.Data.SqlClient;

namespace ClinicManagementSystem.Controllers
{
    public class CustomerController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly string cs;

        public CustomerController(IConfiguration configuration)
        {
            _configuration = configuration;
            cs = _configuration.GetConnectionString("DefaultConnection") 
                 ?? @"Data Source=DESKTOP-J2OEC9R\SQLEXPRESS02;Initial Catalog=ClinicDB;Integrated Security=True;TrustServerCertificate=True";
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (HttpContext.Session.GetString("CustomerId") != null)
            {
                return RedirectToAction("Dashboard");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(CustomerModel model)
        {
            ModelState.Remove("ProfileImage");
            ModelState.Remove("UpdatedDate");

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                using (SqlConnection con = new SqlConnection(cs))
                {
                    con.Open();

                    
                    SqlCommand checkCmd = new SqlCommand(
                        "SELECT COUNT(*) FROM tbl_Customer WHERE Email = @Email AND IsDeleted = 0", con);
                    checkCmd.Parameters.AddWithValue("@Email", model.Email ?? "");

                    int count = Convert.ToInt32(checkCmd.ExecuteScalar());
                    if (count > 0)
                    {
                        ViewBag.Error = "An account with this email address already exists.";
                        return View(model);
                    }

                  
                    string fileName = "";
                    if (model.ImageFile != null && model.ImageFile.Length > 0)
                    {
                        string folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "CustomerProfile");
                        if (!Directory.Exists(folderPath))
                        {
                            Directory.CreateDirectory(folderPath);
                        }

                        fileName = Guid.NewGuid().ToString() + Path.GetExtension(model.ImageFile.FileName);
                        string filePath = Path.Combine(folderPath, fileName);

                        using (FileStream stream = new FileStream(filePath, FileMode.Create))
                        {
                            model.ImageFile.CopyTo(stream);
                        }
                    }

                    string insertQuery = @"
                        INSERT INTO tbl_Customer
                        (
                            FullName, Gender, DateOfBirth, BloodGroup, MobileNo, Email, Password,
                            Address, City, State, Pincode, ProfileImage, IsActive, IsDeleted, CreatedDate
                        )
                        VALUES
                        (
                            @FullName, @Gender, @DateOfBirth, @BloodGroup, @MobileNo, @Email, @Password,
                            @Address, @City, @State, @Pincode, @ProfileImage, 1, 0, GETDATE()
                        )";

                    SqlCommand cmd = new SqlCommand(insertQuery, con);
                    cmd.Parameters.AddWithValue("@FullName", (object?)model.FullName ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Gender", (object?)model.Gender ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@DateOfBirth", (object?)model.DateOfBirth ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@BloodGroup", (object?)model.BloodGroup ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@MobileNo", (object?)model.MobileNo ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Email", (object?)model.Email ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Password", (object?)model.Password ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Address", (object?)model.Address ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@City", (object?)model.City ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@State", (object?)model.State ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Pincode", (object?)model.Pincode ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ProfileImage", string.IsNullOrEmpty(fileName) ? DBNull.Value : fileName);

                    cmd.ExecuteNonQuery();

                    TempData["Success"] = "Registration successful! Please sign in with your credentials.";
                    return RedirectToAction("Login");
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = "An error occurred during registration: " + ex.Message;
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (HttpContext.Session.GetString("CustomerId") != null)
            {
                return RedirectToAction("Dashboard");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(CustomerModel model)
        {
            
            ModelState.Remove("FullName");
            ModelState.Remove("Gender");
            ModelState.Remove("MobileNo");
            ModelState.Remove("ConfirmPassword");
            ModelState.Remove("ProfileImage");
            ModelState.Remove("ImageFile");

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                using (SqlConnection con = new SqlConnection(cs))
                {
                    con.Open();

                    string loginQuery = @"
                        SELECT CustomerId, FullName, Email, ProfileImage, MobileNo
                        FROM tbl_Customer
                        WHERE Email = @Email 
                          AND Password = @Password 
                          AND IsActive = 1 
                          AND IsDeleted = 0";

                    SqlCommand cmd = new SqlCommand(loginQuery, con);
                    cmd.Parameters.AddWithValue("@Email", model.Email ?? "");
                    cmd.Parameters.AddWithValue("@Password", model.Password ?? "");

                    SqlDataReader dr = cmd.ExecuteReader();

                    if (dr.Read())
                    {
                        HttpContext.Session.SetString("CustomerId", dr["CustomerId"].ToString()!);
                        HttpContext.Session.SetString("CustomerName", dr["FullName"].ToString()!);
                        HttpContext.Session.SetString("CustomerEmail", dr["Email"].ToString()!);
                        HttpContext.Session.SetString("CustomerImage", dr["ProfileImage"] != DBNull.Value ? dr["ProfileImage"].ToString()! : "");

                        dr.Close();
                        return RedirectToAction("Dashboard");
                    }
                    else
                    {
                        ViewBag.Error = "Invalid email or password, or account is disabled.";
                        return View(model);
                    }
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = "An error occurred during login: " + ex.Message;
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult Dashboard()
        {
            string? customerIdStr = HttpContext.Session.GetString("CustomerId");
            if (string.IsNullOrEmpty(customerIdStr))
            {
                return RedirectToAction("Login");
            }

            long customerId = Convert.ToInt64(customerIdStr);
            CustomerModel customer = new CustomerModel();

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();
                string query = "SELECT * FROM tbl_Customer WHERE CustomerId = @CustomerId AND IsDeleted = 0";
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@CustomerId", customerId);

                SqlDataReader dr = cmd.ExecuteReader();
                if (dr.Read())
                {
                    customer.CustomerId = Convert.ToInt64(dr["CustomerId"]);
                    customer.FullName = dr["FullName"] != DBNull.Value ? dr["FullName"].ToString() : "";
                    customer.Gender = dr["Gender"] != DBNull.Value ? dr["Gender"].ToString() : "";
                    customer.DateOfBirth = dr["DateOfBirth"] != DBNull.Value ? Convert.ToDateTime(dr["DateOfBirth"]) : null;
                    customer.BloodGroup = dr["BloodGroup"] != DBNull.Value ? dr["BloodGroup"].ToString() : "";
                    customer.MobileNo = dr["MobileNo"] != DBNull.Value ? dr["MobileNo"].ToString() : "";
                    customer.Email = dr["Email"] != DBNull.Value ? dr["Email"].ToString() : "";
                    customer.Address = dr["Address"] != DBNull.Value ? dr["Address"].ToString() : "";
                    customer.City = dr["City"] != DBNull.Value ? dr["City"].ToString() : "";
                    customer.State = dr["State"] != DBNull.Value ? dr["State"].ToString() : "";
                    customer.Pincode = dr["Pincode"] != DBNull.Value ? dr["Pincode"].ToString() : "";
                    customer.ProfileImage = dr["ProfileImage"] != DBNull.Value ? dr["ProfileImage"].ToString() : "";
                    customer.IsActive = dr["IsActive"] != DBNull.Value && Convert.ToBoolean(dr["IsActive"]);
                    customer.CreatedDate = dr["CreatedDate"] != DBNull.Value ? Convert.ToDateTime(dr["CreatedDate"]) : DateTime.Now;
                    if (dr["UpdatedDate"] != DBNull.Value)
                    {
                        customer.UpdatedDate = Convert.ToDateTime(dr["UpdatedDate"]);
                    }
                }
                else
                {
                    dr.Close();
                    HttpContext.Session.Clear();
                    return RedirectToAction("Login");
                }
                dr.Close();

               
                SqlCommand docCountCmd = new SqlCommand("SELECT COUNT(*) FROM tbl_Doctor WHERE IsActive = 1 AND IsDeleted = 0", con);
                ViewBag.TotalDoctors = Convert.ToInt32(docCountCmd.ExecuteScalar());

                SqlCommand deptCountCmd = new SqlCommand("SELECT COUNT(*) FROM tbl_Department WHERE IsActive = 1 AND IsDeleted = 0", con);
                ViewBag.TotalDepartments = Convert.ToInt32(deptCountCmd.ExecuteScalar());
            }

            return View(customer);
        }

        [HttpGet]
        public IActionResult Doctors(string search = "", long departmentId = 0)
        {
            if (HttpContext.Session.GetString("CustomerId") == null)
            {
                return RedirectToAction("Login");
            }

            List<DoctorModel> list = new List<DoctorModel>();

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

                string query = @"

SELECT

d.*,
dep.DepartmentName,

DL.LeaveFromDate,
DL.LeaveToDate

FROM tbl_Doctor d

INNER JOIN tbl_Department dep
ON d.DepartmentId = dep.DepartmentId

LEFT JOIN tbl_DoctorLeave DL
ON d.DoctorId = DL.DoctorId
AND DL.IsActive = 1
AND DL.IsDeleted = 0
AND CAST(GETDATE() AS DATE)
BETWEEN DL.LeaveFromDate AND DL.LeaveToDate

WHERE

d.IsDeleted = 0
AND d.IsActive = 1
";

                if (!string.IsNullOrWhiteSpace(search))
                {
                    query += @"
AND
(
    d.DoctorName LIKE @Search
    OR d.Specialization LIKE @Search
)";
                }

                if (departmentId > 0)
                {
                    query += " AND d.DepartmentId=@DepartmentId";
                }

                query += " ORDER BY d.DoctorName ASC";

                SqlCommand cmd = new SqlCommand(query, con);

                if (!string.IsNullOrWhiteSpace(search))
                {
                    cmd.Parameters.AddWithValue("@Search", "%" + search + "%");
                }

                if (departmentId > 0)
                {
                    cmd.Parameters.AddWithValue("@DepartmentId", departmentId);
                }

                SqlDataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    DoctorModel model = new DoctorModel();

                    model.DoctorId = Convert.ToInt64(dr["DoctorId"]);

                    model.DepartmentId = Convert.ToInt64(dr["DepartmentId"]);

                    model.DepartmentName = dr["DepartmentName"].ToString();

                    model.DoctorName = dr["DoctorName"].ToString();

                    model.Qualification = dr["Qualification"].ToString();

                    model.Specialization = dr["Specialization"].ToString();

                    model.Experience = Convert.ToInt32(dr["Experience"]);

                    model.ConsultationFee = Convert.ToDecimal(dr["ConsultationFee"]);

                    model.MobileNo = dr["MobileNo"].ToString();

                    model.Email = dr["Email"].ToString();

                    model.Gender = dr["Gender"].ToString();

                    model.DoctorImage = dr["DoctorImage"].ToString();

                    if (dr["AvailableFrom"] != DBNull.Value)
                        model.AvailableFrom = (TimeSpan)dr["AvailableFrom"];

                    if (dr["AvailableTo"] != DBNull.Value)
                        model.AvailableTo = (TimeSpan)dr["AvailableTo"];

                    model.Description = dr["Description"].ToString();


                    if (dr["LeaveFromDate"] != DBNull.Value)
                    {
                        model.IsOnLeave = true;

                        model.LeaveFromDate = Convert.ToDateTime(dr["LeaveFromDate"]);

                        model.LeaveToDate = Convert.ToDateTime(dr["LeaveToDate"]);
                    }
                    else
                    {
                        model.IsOnLeave = false;
                    }

                    list.Add(model);
                }

                dr.Close();


                SqlCommand deptCmd = new SqlCommand(@"

SELECT

DepartmentId,
DepartmentName

FROM tbl_Department

WHERE

IsActive=1
AND IsDeleted=0

ORDER BY DepartmentName", con);

                SqlDataReader deptDr = deptCmd.ExecuteReader();

                List<DepartmentModel> departments = new List<DepartmentModel>();

                while (deptDr.Read())
                {
                    departments.Add(new DepartmentModel
                    {
                        DepartmentId = Convert.ToInt64(deptDr["DepartmentId"]),
                        DepartmentName = deptDr["DepartmentName"].ToString()
                    });
                }

                deptDr.Close();

                ViewBag.DepartmentList = departments;

                ViewBag.Search = search;

                ViewBag.DepartmentId = departmentId;
            }

            return View(list);
        }
        [HttpGet]
        public IActionResult DoctorDetails(long id)
        {
            if (HttpContext.Session.GetString("CustomerId") == null)
            {
                return RedirectToAction("Login");
            }

            DoctorModel model = new DoctorModel();

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

                string query = @"
       SELECT
d.*,
dep.DepartmentName,

DL.LeaveFromDate,
DL.LeaveToDate,
DL.LeaveReason

FROM tbl_Doctor d

INNER JOIN tbl_Department dep
ON d.DepartmentId=dep.DepartmentId

LEFT JOIN tbl_DoctorLeave DL
ON d.DoctorId=DL.DoctorId
AND DL.IsActive=1
AND DL.IsDeleted=0
AND CAST(GETDATE() AS DATE)
BETWEEN DL.LeaveFromDate AND DL.LeaveToDate

WHERE

d.DoctorId=@DoctorId
AND d.IsDeleted=0
AND d.IsActive=1";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@DoctorId", id);

                SqlDataReader dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    model.DoctorId = Convert.ToInt64(dr["DoctorId"]);
                    model.DepartmentId = Convert.ToInt64(dr["DepartmentId"]);
                    model.DepartmentName = dr["DepartmentName"].ToString();

                    model.DoctorName = dr["DoctorName"].ToString();
                    model.Qualification = dr["Qualification"].ToString();
                    model.Specialization = dr["Specialization"].ToString();

                    model.Experience = Convert.ToInt32(dr["Experience"]);
                    model.ConsultationFee = Convert.ToDecimal(dr["ConsultationFee"]);

                    model.MobileNo = dr["MobileNo"].ToString();
                    model.Email = dr["Email"].ToString();

                    model.Gender = dr["Gender"].ToString();

                    model.DoctorImage = dr["DoctorImage"].ToString();

                    model.AvailableFrom = dr["AvailableFrom"] != DBNull.Value
                        ? (TimeSpan?)dr["AvailableFrom"]
                        : null;

                    model.AvailableTo = dr["AvailableTo"] != DBNull.Value
                        ? (TimeSpan?)dr["AvailableTo"]
                        : null;

                    model.Description = dr["Description"].ToString();

                    model.IsActive = Convert.ToBoolean(dr["IsActive"]);
                    model.IsDeleted = Convert.ToBoolean(dr["IsDeleted"]);

                    model.CreatedDate = Convert.ToDateTime(dr["CreatedDate"]);

                    if (dr["UpdatedDate"] != DBNull.Value)
                    {
                        model.UpdatedDate = Convert.ToDateTime(dr["UpdatedDate"]);
                    }
                }
                else
                {
                    TempData["Error"] = "Doctor not found.";
                    return RedirectToAction("Doctors");
                }
                //====================================
                // Doctor Leave
                //====================================

                if (dr["LeaveFromDate"] != DBNull.Value)
                {
                    model.IsOnLeave = true;

                    model.LeaveFromDate = Convert.ToDateTime(dr["LeaveFromDate"]);

                    model.LeaveToDate = Convert.ToDateTime(dr["LeaveToDate"]);

                    model.LeaveReason = dr["LeaveReason"].ToString();
                }
                else
                {
                    model.IsOnLeave = false;
                }
                dr.Close();
            }

            return View(model);
        }
     
        [HttpGet]
        public IActionResult BookAppointment(long doctorId)
        {
          
            if (HttpContext.Session.GetString("CustomerId") == null)
            {
                return RedirectToAction("Login");
            }

            AppointmentModel model = new AppointmentModel();

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

                string query = @"
        SELECT d.DoctorId,
               d.DoctorName,
               d.DepartmentId,
               dep.DepartmentName,
               d.ConsultationFee,
               d.AvailableFrom,
               d.AvailableTo
        FROM tbl_Doctor d
        INNER JOIN tbl_Department dep
            ON d.DepartmentId = dep.DepartmentId
        WHERE d.DoctorId=@DoctorId
        AND d.IsActive=1
        AND d.IsDeleted=0";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@DoctorId", doctorId);

                SqlDataReader dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    model.DoctorId = Convert.ToInt64(dr["DoctorId"]);
                    model.DoctorName = dr["DoctorName"].ToString();

                    model.Department = dr["DepartmentName"].ToString();

                    model.ConsultationFee = Convert.ToDecimal(dr["ConsultationFee"]);

                    if (dr["AvailableFrom"] != DBNull.Value)
                    {
                        model.AvailableFrom = (TimeSpan)dr["AvailableFrom"];
                    }

                    if (dr["AvailableTo"] != DBNull.Value)
                    {
                        model.AvailableTo = (TimeSpan)dr["AvailableTo"];
                    }
                }
                else
                {
                    TempData["Error"] = "Doctor not found.";
                    return RedirectToAction("Doctors");
                }

                dr.Close();
            }

           
            model.AppointmentDate = DateTime.Today.AddDays(1);
            model.AppointmentStatus = "Pending";
            model.PaymentStatus = "Pending";

            return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult BookAppointment(AppointmentModel model)
        {
            if (HttpContext.Session.GetString("CustomerId") == null)
            {
                return RedirectToAction("Login");
            }

            model.CustomerId = Convert.ToInt64(HttpContext.Session.GetString("CustomerId"));


            ModelState.Remove(nameof(AppointmentModel.CustomerId));
            ModelState.Remove(nameof(AppointmentModel.DoctorName));
            ModelState.Remove(nameof(AppointmentModel.CustomerName));
            ModelState.Remove(nameof(AppointmentModel.DoctorImage));
            ModelState.Remove(nameof(AppointmentModel.DepartmentName));
            ModelState.Remove(nameof(AppointmentModel.Prescription));
            ModelState.Remove(nameof(AppointmentModel.PrescriptionFile));
            ModelState.Remove(nameof(AppointmentModel.AppointmentNo));
            ModelState.Remove(nameof(AppointmentModel.AdminRemark));
            ModelState.Remove(nameof(AppointmentModel.CustomerRemark));
            ModelState.Remove(nameof(AppointmentModel.MobileNo));
            ModelState.Remove(nameof(AppointmentModel.Email));
            ModelState.Remove(nameof(AppointmentModel.Qualification));
            ModelState.Remove(nameof(AppointmentModel.Specialization));
            ModelState.Remove(nameof(AppointmentModel.Experience));
            ModelState.Remove(nameof(AppointmentModel.AvailableFrom));
            ModelState.Remove(nameof(AppointmentModel.AvailableTo));


            if (!ModelState.IsValid)
            {
                using (SqlConnection con = new SqlConnection(cs))
                {
                    con.Open();

                    string doctorQuery = @"

SELECT

d.DoctorName,
d.DoctorImage,
dep.DepartmentName,
d.ConsultationFee,
d.AvailableFrom,
d.AvailableTo

FROM tbl_Doctor d

INNER JOIN tbl_Department dep
ON d.DepartmentId = dep.DepartmentId

WHERE d.DoctorId=@DoctorId";

                    SqlCommand cmd = new SqlCommand(doctorQuery, con);

                    cmd.Parameters.AddWithValue("@DoctorId", model.DoctorId);

                    SqlDataReader dr = cmd.ExecuteReader();

                    if (dr.Read())
                    {
                        model.DoctorName = dr["DoctorName"].ToString();

                        model.DoctorImage = dr["DoctorImage"].ToString();

                        model.Department = dr["DepartmentName"].ToString();

                        model.ConsultationFee = Convert.ToDecimal(dr["ConsultationFee"]);

                        if (dr["AvailableFrom"] != DBNull.Value)
                        {
                            model.AvailableFrom = (TimeSpan)dr["AvailableFrom"];
                        }

                        if (dr["AvailableTo"] != DBNull.Value)
                        {
                            model.AvailableTo = (TimeSpan)dr["AvailableTo"];
                        }
                    }

                    dr.Close();
                }

                return View(model);
            }
            try
            {
                using (SqlConnection con = new SqlConnection(cs))
                {
                    con.Open();


                    string leaveQuery = @"

SELECT

LeaveFromDate,
LeaveToDate

FROM tbl_DoctorLeave

WHERE

DoctorId=@DoctorId
AND IsActive=1
AND IsDeleted=0

AND

@AppointmentDate BETWEEN LeaveFromDate AND LeaveToDate";

                    SqlCommand leaveCmd = new SqlCommand(leaveQuery, con);

                    leaveCmd.Parameters.AddWithValue("@DoctorId", model.DoctorId);

                    leaveCmd.Parameters.AddWithValue("@AppointmentDate", model.AppointmentDate.Value.Date);

                    SqlDataReader leaveDr = leaveCmd.ExecuteReader();

                    if (leaveDr.Read())
                    {
                        DateTime fromDate = Convert.ToDateTime(leaveDr["LeaveFromDate"]);

                        DateTime toDate = Convert.ToDateTime(leaveDr["LeaveToDate"]);

                        leaveDr.Close();

                        ModelState.AddModelError("", $"Doctor is on leave from {fromDate:dd MMM yyyy} to {toDate:dd MMM yyyy}. Please select another appointment date.");

                        con.Close();

                        using (SqlConnection con1 = new SqlConnection(cs))
                        {
                            con1.Open();

                            SqlCommand cmd1 = new SqlCommand(@"
SELECT
d.DoctorName,
d.DoctorImage,
dep.DepartmentName,
d.ConsultationFee,
d.AvailableFrom,
d.AvailableTo

FROM tbl_Doctor d

INNER JOIN tbl_Department dep
ON d.DepartmentId=dep.DepartmentId

WHERE d.DoctorId=@DoctorId", con1);

                            cmd1.Parameters.AddWithValue("@DoctorId", model.DoctorId);

                            SqlDataReader dr = cmd1.ExecuteReader();

                            if (dr.Read())
                            {
                                model.DoctorName = dr["DoctorName"].ToString();
                                model.DoctorImage = dr["DoctorImage"].ToString();
                                model.Department = dr["DepartmentName"].ToString();
                                model.ConsultationFee = Convert.ToDecimal(dr["ConsultationFee"]);

                                if (dr["AvailableFrom"] != DBNull.Value)
                                    model.AvailableFrom = (TimeSpan)dr["AvailableFrom"];

                                if (dr["AvailableTo"] != DBNull.Value)
                                    model.AvailableTo = (TimeSpan)dr["AvailableTo"];
                            }

                            dr.Close();
                        }

                        return View(model);
                    }

                    leaveDr.Close();

                    //=========================================
                    // Check Hospital Holiday
                    //=========================================

                    string holidayQuery = @"

SELECT

HolidayTitle

FROM tbl_HospitalHoliday

WHERE

HolidayDate=@HolidayDate
AND IsActive=1
AND IsDeleted=0";

                    SqlCommand holidayCmd = new SqlCommand(holidayQuery, con);

                    holidayCmd.Parameters.AddWithValue("@HolidayDate", model.AppointmentDate.Value.Date);

                    object holidayResult = holidayCmd.ExecuteScalar();

                    if (holidayResult != null)
                    {
                        string holidayTitle = holidayResult.ToString();

                        ModelState.AddModelError("",
                            $"Hospital is closed on {model.AppointmentDate.Value:dd MMM yyyy} due to '{holidayTitle}'. Please select another appointment date.");

                        con.Close();

                        using (SqlConnection con1 = new SqlConnection(cs))
                        {
                            con1.Open();

                            SqlCommand cmd1 = new SqlCommand(@"

SELECT

d.DoctorName,
d.DoctorImage,
dep.DepartmentName,
d.ConsultationFee,
d.AvailableFrom,
d.AvailableTo

FROM tbl_Doctor d

INNER JOIN tbl_Department dep
ON d.DepartmentId = dep.DepartmentId

WHERE d.DoctorId=@DoctorId", con1);

                            cmd1.Parameters.AddWithValue("@DoctorId", model.DoctorId);

                            SqlDataReader dr = cmd1.ExecuteReader();

                            if (dr.Read())
                            {
                                model.DoctorName = dr["DoctorName"].ToString();
                                model.DoctorImage = dr["DoctorImage"].ToString();
                                model.Department = dr["DepartmentName"].ToString();
                                model.ConsultationFee = Convert.ToDecimal(dr["ConsultationFee"]);

                                if (dr["AvailableFrom"] != DBNull.Value)
                                    model.AvailableFrom = (TimeSpan)dr["AvailableFrom"];

                                if (dr["AvailableTo"] != DBNull.Value)
                                    model.AvailableTo = (TimeSpan)dr["AvailableTo"];
                            }

                            dr.Close();
                        }

                        return View(model);
                    }

                    string appointmentNo = "APT" + DateTime.Now.ToString("yyyyMMddHHmmss");


                    SqlCommand cmd = new SqlCommand(@"

INSERT INTO tbl_Appointment
(
AppointmentNo,
CustomerId,
DoctorId,
Department,
AppointmentDate,
AppointmentTime,
Symptoms,
ConsultationFee,
AppointmentStatus,
PaymentStatus,
IsActive,
IsDeleted,
CreatedDate
)

VALUES
(
@AppointmentNo,
@CustomerId,
@DoctorId,
@Department,
@AppointmentDate,
@AppointmentTime,
@Symptoms,
@ConsultationFee,
@AppointmentStatus,
@PaymentStatus,
@IsActive,
@IsDeleted,
@CreatedDate
)", con);

                    cmd.Parameters.AddWithValue("@AppointmentNo", appointmentNo);

                    cmd.Parameters.AddWithValue("@CustomerId", model.CustomerId);

                    cmd.Parameters.AddWithValue("@DoctorId", model.DoctorId);

                    cmd.Parameters.AddWithValue("@Department", model.Department);

                    cmd.Parameters.AddWithValue("@AppointmentDate", model.AppointmentDate);

                    cmd.Parameters.AddWithValue("@AppointmentTime", model.AppointmentTime);

                    cmd.Parameters.AddWithValue("@Symptoms", model.Symptoms);

                    cmd.Parameters.AddWithValue("@ConsultationFee", model.ConsultationFee);

                    cmd.Parameters.AddWithValue("@AppointmentStatus", "Pending");

                    cmd.Parameters.AddWithValue("@PaymentStatus", "Pending");

                    cmd.Parameters.AddWithValue("@IsActive", true);

                    cmd.Parameters.AddWithValue("@IsDeleted", false);

                    cmd.Parameters.AddWithValue("@CreatedDate", DateTime.Now);

                    int result = cmd.ExecuteNonQuery();

                    if (result > 0)
                    {
                        TempData["Success"] = "Appointment booked successfully.";

                        return RedirectToAction("MyAppointments");
                    }

                    TempData["Error"] = "Appointment booking failed.";

                    return View(model);
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;

                using (SqlConnection con = new SqlConnection(cs))
                {
                    con.Open();

                    string doctorQuery = @"

SELECT

d.DoctorName,
d.DoctorImage,
dep.DepartmentName,
d.ConsultationFee,
d.AvailableFrom,
d.AvailableTo

FROM tbl_Doctor d

INNER JOIN tbl_Department dep
ON d.DepartmentId = dep.DepartmentId

WHERE d.DoctorId=@DoctorId";

                    SqlCommand cmd = new SqlCommand(doctorQuery, con);

                    cmd.Parameters.AddWithValue("@DoctorId", model.DoctorId);

                    SqlDataReader dr = cmd.ExecuteReader();

                    if (dr.Read())
                    {
                        model.DoctorName = dr["DoctorName"].ToString();

                        model.DoctorImage = dr["DoctorImage"].ToString();

                        model.Department = dr["DepartmentName"].ToString();

                        model.ConsultationFee = Convert.ToDecimal(dr["ConsultationFee"]);

                        if (dr["AvailableFrom"] != DBNull.Value)
                        {
                            model.AvailableFrom = (TimeSpan)dr["AvailableFrom"];
                        }

                        if (dr["AvailableTo"] != DBNull.Value)
                        {
                            model.AvailableTo = (TimeSpan)dr["AvailableTo"];
                        }
                    }

                    dr.Close();
                }

                return View(model);
            }
        }

        [HttpGet]
        public IActionResult MyAppointments(string search = "")
        {
            if (HttpContext.Session.GetString("CustomerId") == null)
            {
                return RedirectToAction("Login");
            }

            long customerId = Convert.ToInt64(HttpContext.Session.GetString("CustomerId"));

            List<AppointmentModel> list = new List<AppointmentModel>();

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

                string query = @"
        SELECT
            a.AppointmentId,
            a.AppointmentNo,
            a.CustomerId,
            a.DoctorId,
            a.Department,
            a.AppointmentDate,
            a.AppointmentTime,
            a.Symptoms,
            a.ConsultationFee,
            a.AppointmentStatus,
            a.PaymentStatus,
            a.PrescriptionFile,
            a.AdminRemark,
            a.CustomerRemark,
            a.CreatedDate,

            d.DoctorName,
            d.DoctorImage

        FROM tbl_Appointment a

        INNER JOIN tbl_Doctor d
            ON a.DoctorId = d.DoctorId

        WHERE a.CustomerId = @CustomerId
        AND a.IsDeleted = 0
        AND
        (
            d.DoctorName LIKE @Search
            OR
            a.AppointmentNo LIKE @Search
            OR
            a.AppointmentStatus LIKE @Search
        )

        ORDER BY a.CreatedDate DESC";

                SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@CustomerId", customerId);
                cmd.Parameters.AddWithValue("@Search", "%" + search + "%");

                SqlDataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    AppointmentModel model = new AppointmentModel();

                    model.AppointmentId = Convert.ToInt64(dr["AppointmentId"]);
                    model.AppointmentNo = dr["AppointmentNo"].ToString();

                    model.CustomerId = Convert.ToInt64(dr["CustomerId"]);
                    model.DoctorId = Convert.ToInt64(dr["DoctorId"]);

                    model.DoctorName = dr["DoctorName"].ToString();
                    model.DoctorImage = dr["DoctorImage"].ToString();

                    model.Department = dr["Department"].ToString();

                    model.AppointmentDate = Convert.ToDateTime(dr["AppointmentDate"]);

                    if (dr["AppointmentTime"] != DBNull.Value)
                    {
                        model.AppointmentTime = (TimeSpan)dr["AppointmentTime"];
                    }

                    model.Symptoms = dr["Symptoms"].ToString();

                    model.ConsultationFee = Convert.ToDecimal(dr["ConsultationFee"]);

                    model.AppointmentStatus = dr["AppointmentStatus"].ToString();

                    model.PaymentStatus = dr["PaymentStatus"].ToString();

                    model.PrescriptionFile = dr["PrescriptionFile"].ToString();

                    model.AdminRemark = dr["AdminRemark"].ToString();

                    model.CustomerRemark = dr["CustomerRemark"].ToString();

                    model.CreatedDate = Convert.ToDateTime(dr["CreatedDate"]);

                    list.Add(model);
                }

                dr.Close();
            }

            ViewBag.Search = search;

            return View(list);
        }
  
        [HttpGet]
        public IActionResult AppointmentDetails(long id)
        {
          
            if (HttpContext.Session.GetString("CustomerId") == null)
            {
                return RedirectToAction("Login");
            }

            long customerId = Convert.ToInt64(HttpContext.Session.GetString("CustomerId"));

            AppointmentModel model = new AppointmentModel();

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

                string query = @"
        SELECT
            a.*,
            d.DoctorName,
            d.DoctorImage,
            d.Qualification,
            d.Specialization,
            dep.DepartmentName

        FROM tbl_Appointment a

        INNER JOIN tbl_Doctor d
            ON a.DoctorId = d.DoctorId

        INNER JOIN tbl_Department dep
            ON d.DepartmentId = dep.DepartmentId

        WHERE a.AppointmentId=@AppointmentId
        AND a.CustomerId=@CustomerId
        AND a.IsDeleted=0";

                SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@AppointmentId", id);
                cmd.Parameters.AddWithValue("@CustomerId", customerId);

                SqlDataReader dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    model.AppointmentId = Convert.ToInt64(dr["AppointmentId"]);
                    model.AppointmentNo = dr["AppointmentNo"].ToString();

                    model.CustomerId = Convert.ToInt64(dr["CustomerId"]);
                    model.DoctorId = Convert.ToInt64(dr["DoctorId"]);

                    model.DoctorName = dr["DoctorName"].ToString();
                    model.DoctorImage = dr["DoctorImage"].ToString();

                    model.Department = dr["DepartmentName"].ToString();

                    model.Qualification = dr["Qualification"].ToString();
                    model.Specialization = dr["Specialization"].ToString();

                    if (dr["AppointmentDate"] != DBNull.Value)
                        model.AppointmentDate = Convert.ToDateTime(dr["AppointmentDate"]);

                    if (dr["AppointmentTime"] != DBNull.Value)
                        model.AppointmentTime = (TimeSpan?)dr["AppointmentTime"];

                    model.Symptoms = dr["Symptoms"].ToString();

                    model.ConsultationFee = Convert.ToDecimal(dr["ConsultationFee"]);

                    model.AppointmentStatus = dr["AppointmentStatus"].ToString();

                    model.PaymentStatus = dr["PaymentStatus"].ToString();

                    model.PrescriptionFile = dr["PrescriptionFile"].ToString();

                    model.AdminRemark = dr["AdminRemark"].ToString();

                    model.CustomerRemark = dr["CustomerRemark"].ToString();

                    model.IsActive = Convert.ToBoolean(dr["IsActive"]);
                    model.IsDeleted = Convert.ToBoolean(dr["IsDeleted"]);

                    model.CreatedDate = Convert.ToDateTime(dr["CreatedDate"]);

                    if (dr["UpdatedDate"] != DBNull.Value)
                    {
                        model.UpdatedDate = Convert.ToDateTime(dr["UpdatedDate"]);
                    }
                }
                else
                {
                    TempData["Error"] = "Appointment not found.";
                    return RedirectToAction("MyAppointments");
                }

                dr.Close();
            }

            return View(model);
        }
        
        [HttpGet]
        public IActionResult CancelAppointment(long id)
        {
          
            if (HttpContext.Session.GetString("CustomerId") == null)
            {
                return RedirectToAction("Login");
            }

            long customerId = Convert.ToInt64(HttpContext.Session.GetString("CustomerId"));

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

                
                string checkQuery = @"
        SELECT AppointmentStatus
        FROM tbl_Appointment
        WHERE AppointmentId=@AppointmentId
        AND CustomerId=@CustomerId
        AND IsDeleted=0";

                SqlCommand checkCmd = new SqlCommand(checkQuery, con);

                checkCmd.Parameters.AddWithValue("@AppointmentId", id);
                checkCmd.Parameters.AddWithValue("@CustomerId", customerId);

                object statusObj = checkCmd.ExecuteScalar();

                if (statusObj == null)
                {
                    TempData["Error"] = "Appointment not found.";
                    return RedirectToAction("MyAppointments");
                }

                string status = statusObj.ToString();

                
                if (status != "Pending" && status != "Approved")
                {
                    TempData["Error"] = "Only Pending or Approved appointments can be cancelled.";
                    return RedirectToAction("MyAppointments");
                }

              
                string updateQuery = @"
        UPDATE tbl_Appointment
        SET AppointmentStatus=@AppointmentStatus,
            UpdatedDate=@UpdatedDate
        WHERE AppointmentId=@AppointmentId
        AND CustomerId=@CustomerId";

                SqlCommand updateCmd = new SqlCommand(updateQuery, con);

                updateCmd.Parameters.AddWithValue("@AppointmentStatus", "Cancelled");
                updateCmd.Parameters.AddWithValue("@UpdatedDate", DateTime.Now);
                updateCmd.Parameters.AddWithValue("@AppointmentId", id);
                updateCmd.Parameters.AddWithValue("@CustomerId", customerId);

                int result = updateCmd.ExecuteNonQuery();

                if (result > 0)
                {
                    TempData["Success"] = "Appointment cancelled successfully.";
                }
                else
                {
                    TempData["Error"] = "Unable to cancel appointment.";
                }
            }

            return RedirectToAction("MyAppointments");
        }
 
        [HttpGet]
        public IActionResult PayNow(long id)
        {
            if (HttpContext.Session.GetString("CustomerId") == null)
            {
                return RedirectToAction("Login");
            }

            long customerId = Convert.ToInt64(HttpContext.Session.GetString("CustomerId"));

            PaymentModel model = new PaymentModel();

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

                string query = @"
SELECT

    a.AppointmentId,
    a.AppointmentNo,
    a.CustomerId,
    a.AppointmentDate,
    a.AppointmentTime,
    a.ConsultationFee,
    a.PaymentStatus,

    c.FullName,
    c.MobileNo,
    c.Email,

    d.DoctorName,
    d.DoctorImage,

    dep.DepartmentName

FROM tbl_Appointment a

INNER JOIN tbl_Customer c
    ON a.CustomerId = c.CustomerId

INNER JOIN tbl_Doctor d
    ON a.DoctorId = d.DoctorId

INNER JOIN tbl_Department dep
    ON d.DepartmentId = dep.DepartmentId

WHERE
    a.AppointmentId = @AppointmentId
    AND a.CustomerId = @CustomerId
    AND a.IsDeleted = 0";

                SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@AppointmentId", id);
                cmd.Parameters.AddWithValue("@CustomerId", customerId);

                SqlDataReader dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    model.AppointmentId = Convert.ToInt64(dr["AppointmentId"]);
                    model.AppointmentNo = dr["AppointmentNo"].ToString();

                    model.CustomerId = Convert.ToInt64(dr["CustomerId"]);

                    model.CustomerName = dr["FullName"].ToString();
                    model.MobileNo = dr["MobileNo"].ToString();
                    model.Email = dr["Email"].ToString();

                    model.DoctorName = dr["DoctorName"].ToString();
                    model.DoctorImage = dr["DoctorImage"].ToString();
                    model.DepartmentName = dr["DepartmentName"].ToString();

                    if (dr["AppointmentDate"] != DBNull.Value)
                        model.AppointmentDate = Convert.ToDateTime(dr["AppointmentDate"]);

                    if (dr["AppointmentTime"] != DBNull.Value)
                        model.AppointmentTime = (TimeSpan)dr["AppointmentTime"];

                    model.Amount = Convert.ToDecimal(dr["ConsultationFee"]);

                    model.ConsultationFee = Convert.ToDecimal(dr["ConsultationFee"]);

                    model.PaymentStatus = dr["PaymentStatus"].ToString();
                }

                dr.Close();
            }

            if (model.AppointmentId == 0)
            {
                TempData["Error"] = "Appointment not found.";
                return RedirectToAction("MyAppointments");
            }

            if (model.PaymentStatus == "Paid")
            {
                TempData["Error"] = "Payment has already been completed.";
                return RedirectToAction("MyAppointments");
            }

            return View(model);
        }
      
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult PayNow(PaymentModel model)
        {
           
            if (HttpContext.Session.GetString("CustomerId") == null)
            {
                return RedirectToAction("Login");
            }

            model.CustomerId = Convert.ToInt64(HttpContext.Session.GetString("CustomerId"));

            ModelState.Remove("CustomerName");
            ModelState.Remove("DoctorName");
            ModelState.Remove("DoctorImage");
            ModelState.Remove("DepartmentName");
            ModelState.Remove("AppointmentNo");
            ModelState.Remove("MobileNo");
            ModelState.Remove("Email");
            ModelState.Remove("AppointmentDate");
            ModelState.Remove("AppointmentTime");
            ModelState.Remove("ConsultationFee");
            ModelState.Remove("PaymentStatus");
            ModelState.Remove("PaymentDate");
            ModelState.Remove("ReceiptNo");

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

                SqlTransaction transaction = con.BeginTransaction();

                try
                {
                   
                    SqlCommand checkCmd = new SqlCommand(@"
            SELECT PaymentStatus
            FROM tbl_Appointment
            WHERE AppointmentId=@AppointmentId",
                    con, transaction);

                    checkCmd.Parameters.AddWithValue("@AppointmentId", model.AppointmentId);

                    object status = checkCmd.ExecuteScalar();

                    if (status != null && status.ToString() == "Paid")
                    {
                        transaction.Rollback();

                        TempData["Error"] = "Payment has already been completed.";

                        return RedirectToAction("MyAppointments");
                    }

                    
                    if (string.IsNullOrWhiteSpace(model.TransactionId))
                    {
                        model.TransactionId = "TXN" + DateTime.Now.ToString("yyyyMMddHHmmss");
                    }

                   
                    SqlCommand cmd = new SqlCommand(@"
            INSERT INTO tbl_Payment
            (
                AppointmentId,
                CustomerId,
                Amount,
                PaymentMethod,
                TransactionId,
                PaymentStatus,
                PaymentDate,
                CreatedDate
            )
            VALUES
            (
                @AppointmentId,
                @CustomerId,
                @Amount,
                @PaymentMethod,
                @TransactionId,
                @PaymentStatus,
                @PaymentDate,
                @CreatedDate
            )",
                    con, transaction);

                    cmd.Parameters.AddWithValue("@AppointmentId", model.AppointmentId);
                    cmd.Parameters.AddWithValue("@CustomerId", model.CustomerId);
                    cmd.Parameters.AddWithValue("@Amount", model.Amount);
                    cmd.Parameters.AddWithValue("@PaymentMethod", model.PaymentMethod);
                    cmd.Parameters.AddWithValue("@TransactionId", model.TransactionId);
                    cmd.Parameters.AddWithValue("@PaymentStatus", "Paid");
                    cmd.Parameters.AddWithValue("@PaymentDate", DateTime.Now);
                    cmd.Parameters.AddWithValue("@CreatedDate", DateTime.Now);

                    cmd.ExecuteNonQuery();

                    SqlCommand updateCmd = new SqlCommand(@"
            UPDATE tbl_Appointment
            SET
                PaymentStatus='Paid',
                UpdatedDate=@UpdatedDate
            WHERE AppointmentId=@AppointmentId",
                    con, transaction);

                    updateCmd.Parameters.AddWithValue("@UpdatedDate", DateTime.Now);
                    updateCmd.Parameters.AddWithValue("@AppointmentId", model.AppointmentId);

                    updateCmd.ExecuteNonQuery();

                    transaction.Commit();

                    TempData["Success"] = "Payment completed successfully.";

                    return RedirectToAction("PaymentHistory");
                }
                catch (Exception)
                {
                    transaction.Rollback();

                    TempData["Error"] = "Payment failed.";

                    return View(model);
                }
            }
        }
        
        [HttpGet]
        public IActionResult PaymentHistory(string search = "")
        {
            if (HttpContext.Session.GetString("CustomerId") == null)
            {
                return RedirectToAction("Login");
            }

            long customerId = Convert.ToInt64(HttpContext.Session.GetString("CustomerId"));

            List<PaymentModel> list = new List<PaymentModel>();

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

                string query = @"
SELECT

    p.PaymentId,
    p.AppointmentId,
    p.Amount,
    p.PaymentMethod,
    p.TransactionId,
    p.PaymentStatus,
    p.PaymentDate,
    p.CreatedDate,

    a.AppointmentNo,

    c.FullName,

    d.DoctorName,

    dep.DepartmentName

FROM tbl_Payment p

INNER JOIN tbl_Appointment a
    ON p.AppointmentId = a.AppointmentId

INNER JOIN tbl_Customer c
    ON p.CustomerId = c.CustomerId

INNER JOIN tbl_Doctor d
    ON a.DoctorId = d.DoctorId

INNER JOIN tbl_Department dep
    ON d.DepartmentId = dep.DepartmentId

WHERE
    p.CustomerId = @CustomerId

AND
(
    a.AppointmentNo LIKE @Search
    OR
    d.DoctorName LIKE @Search
    OR
    p.TransactionId LIKE @Search
)

ORDER BY p.PaymentDate DESC";

                SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@CustomerId", customerId);
                cmd.Parameters.AddWithValue("@Search", "%" + search + "%");

                SqlDataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    PaymentModel model = new PaymentModel();

                    model.PaymentId = Convert.ToInt64(dr["PaymentId"]);

                    model.AppointmentId = Convert.ToInt64(dr["AppointmentId"]);

                    model.AppointmentNo = dr["AppointmentNo"].ToString();

                    model.CustomerName = dr["FullName"].ToString();

                    model.DoctorName = dr["DoctorName"].ToString();

                    model.DepartmentName = dr["DepartmentName"].ToString();

                    model.Amount = Convert.ToDecimal(dr["Amount"]);

                    model.PaymentMethod = dr["PaymentMethod"].ToString();

                    model.TransactionId = dr["TransactionId"].ToString();

                    model.PaymentStatus = dr["PaymentStatus"].ToString();

                    if (dr["PaymentDate"] != DBNull.Value)
                        model.PaymentDate = Convert.ToDateTime(dr["PaymentDate"]);

                    model.CreatedDate = Convert.ToDateTime(dr["CreatedDate"]);

                    list.Add(model);
                }

                dr.Close();
            }

            ViewBag.Search = search;

            return View(list);
        }
       
        [HttpGet]
        public IActionResult ViewPayment(long id)
        {
         
            if (HttpContext.Session.GetString("CustomerId") == null)
            {
                return RedirectToAction("Login");
            }

            long customerId = Convert.ToInt64(HttpContext.Session.GetString("CustomerId"));

            PaymentModel model = new PaymentModel();

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

                string query = @"
SELECT

    p.*,

    a.AppointmentNo,
    a.AppointmentDate,
    a.AppointmentTime,

    c.FullName,
    c.MobileNo,
    c.Email,

    d.DoctorName,
    d.DoctorImage,

    dep.DepartmentName

FROM tbl_Payment p

INNER JOIN tbl_Appointment a
    ON p.AppointmentId = a.AppointmentId

INNER JOIN tbl_Customer c
    ON p.CustomerId = c.CustomerId

INNER JOIN tbl_Doctor d
    ON a.DoctorId = d.DoctorId

INNER JOIN tbl_Department dep
    ON d.DepartmentId = dep.DepartmentId

WHERE
    p.PaymentId=@PaymentId
    AND p.CustomerId=@CustomerId";

                SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@PaymentId", id);
                cmd.Parameters.AddWithValue("@CustomerId", customerId);

                SqlDataReader dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    model.PaymentId = Convert.ToInt64(dr["PaymentId"]);

                    model.AppointmentId = Convert.ToInt64(dr["AppointmentId"]);

                    model.CustomerId = Convert.ToInt64(dr["CustomerId"]);

                    model.AppointmentNo = dr["AppointmentNo"].ToString();

                    model.CustomerName = dr["FullName"].ToString();

                    model.MobileNo = dr["MobileNo"].ToString();

                    model.Email = dr["Email"].ToString();

                    model.DoctorName = dr["DoctorName"].ToString();

                    model.DoctorImage = dr["DoctorImage"].ToString();

                    model.DepartmentName = dr["DepartmentName"].ToString();

                    model.Amount = Convert.ToDecimal(dr["Amount"]);

                    model.PaymentMethod = dr["PaymentMethod"].ToString();

                    model.TransactionId = dr["TransactionId"].ToString();

                    model.PaymentStatus = dr["PaymentStatus"].ToString();

                    if (dr["AppointmentDate"] != DBNull.Value)
                        model.AppointmentDate = Convert.ToDateTime(dr["AppointmentDate"]);

                    if (dr["AppointmentTime"] != DBNull.Value)
                        model.AppointmentTime = (TimeSpan)dr["AppointmentTime"];

                    if (dr["PaymentDate"] != DBNull.Value)
                        model.PaymentDate = Convert.ToDateTime(dr["PaymentDate"]);

                    model.CreatedDate = Convert.ToDateTime(dr["CreatedDate"]);

                    if (dr["UpdatedDate"] != DBNull.Value)
                        model.UpdatedDate = Convert.ToDateTime(dr["UpdatedDate"]);
                }

                dr.Close();
            }

            if (model.PaymentId == 0)
            {
                TempData["Error"] = "Payment record not found.";

                return RedirectToAction("PaymentHistory");
            }

            return View(model);
        }
    
        [HttpGet]
        public IActionResult DownloadReceipt(long id)
        {
            if (HttpContext.Session.GetString("CustomerId") == null)
            {
                return RedirectToAction("Login");
            }

            long customerId = Convert.ToInt64(HttpContext.Session.GetString("CustomerId"));

            PaymentModel model = new PaymentModel();

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

                string query = @"
SELECT

    p.*,

    a.AppointmentNo,
    a.AppointmentDate,
    a.AppointmentTime,

    c.FullName,
    c.MobileNo,
    c.Email,

    d.DoctorName,

    dep.DepartmentName

FROM tbl_Payment p

INNER JOIN tbl_Appointment a
ON p.AppointmentId = a.AppointmentId

INNER JOIN tbl_Customer c
ON p.CustomerId = c.CustomerId

INNER JOIN tbl_Doctor d
ON a.DoctorId = d.DoctorId

INNER JOIN tbl_Department dep
ON d.DepartmentId = dep.DepartmentId

WHERE
p.PaymentId=@PaymentId
AND
p.CustomerId=@CustomerId";

                SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@PaymentId", id);
                cmd.Parameters.AddWithValue("@CustomerId", customerId);

                SqlDataReader dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    model.PaymentId = Convert.ToInt64(dr["PaymentId"]);
                    model.AppointmentNo = dr["AppointmentNo"].ToString();
                    model.CustomerName = dr["FullName"].ToString();
                    model.MobileNo = dr["MobileNo"].ToString();
                    model.Email = dr["Email"].ToString();
                    model.DoctorName = dr["DoctorName"].ToString();
                    model.DepartmentName = dr["DepartmentName"].ToString();
                    model.Amount = Convert.ToDecimal(dr["Amount"]);
                    model.PaymentMethod = dr["PaymentMethod"].ToString();
                    model.TransactionId = dr["TransactionId"].ToString();
                    model.PaymentStatus = dr["PaymentStatus"].ToString();

                    if (dr["PaymentDate"] != DBNull.Value)
                        model.PaymentDate = Convert.ToDateTime(dr["PaymentDate"]);

                    if (dr["AppointmentDate"] != DBNull.Value)
                        model.AppointmentDate = Convert.ToDateTime(dr["AppointmentDate"]);

                    if (dr["AppointmentTime"] != DBNull.Value)
                        model.AppointmentTime = (TimeSpan)dr["AppointmentTime"];
                }

                dr.Close();
            }

            if (model.PaymentId == 0)
            {
                TempData["Error"] = "Payment not found.";
                return RedirectToAction("PaymentHistory");
            }

            byte[] pdf = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);

                    page.Header().Text("CLINIC PAYMENT RECEIPT")
                        .FontSize(22)
                        .Bold()
                        .FontColor(Colors.Blue.Darken2);

                    page.Content().Column(col =>
                    {
                        col.Spacing(10);

                        col.Item().Text($"Receipt No : {model.PaymentId}").Bold();

                        col.Item().Text($"Appointment No : {model.AppointmentNo}");

                        col.Item().Text($"Customer : {model.CustomerName}");

                        col.Item().Text($"Mobile : {model.MobileNo}");

                        col.Item().Text($"Email : {model.Email}");

                        col.Item().Text($"Doctor : Dr. {model.DoctorName}");

                        col.Item().Text($"Department : {model.DepartmentName}");

                        col.Item().Text($"Appointment Date : {model.AppointmentDate:dd MMM yyyy}");

                        col.Item().Text($"Appointment Time : {model.AppointmentTime}");

                        col.Item().Text($"Amount : ? {model.Amount:N2}");

                        col.Item().Text($"Payment Method : {model.PaymentMethod}");

                        col.Item().Text($"Transaction ID : {model.TransactionId}");

                        col.Item().Text($"Payment Status : {model.PaymentStatus}");

                        col.Item().Text($"Payment Date : {model.PaymentDate:dd MMM yyyy hh:mm tt}");

                        col.Item().PaddingTop(20);

                        col.Item().Text("----------------------------------------");

                        col.Item().Text("Thank You For Your Payment")
                            .FontSize(18)
                            .Bold()
                            .FontColor(Colors.Green.Darken2);

                        col.Item().Text("Clinic Management System");
                    });

                    page.Footer()
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.Span("Generated on ");
                            x.Span(DateTime.Now.ToString("dd MMM yyyy hh:mm tt"));
                        });
                });
            }).GeneratePdf();

            return File(pdf,
                "application/pdf",
                "PaymentReceipt_" + model.PaymentId + ".pdf");
        }
       
        [HttpGet]
        public IActionResult MyPrescriptions(string search = "", string status = "")
        {
           
            if (HttpContext.Session.GetString("CustomerId") == null)
            {
                return RedirectToAction("Login");
            }

            long customerId = Convert.ToInt64(HttpContext.Session.GetString("CustomerId"));

            List<PrescriptionModel> list = new List<PrescriptionModel>();

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

                string query = @"

SELECT

    p.PrescriptionId,
    p.AppointmentId,
    p.Diagnosis,
    p.Medicines,
    p.NextVisitDate,
    p.PrescriptionStatus,
    p.CreatedDate,

    a.AppointmentNo,
    a.AppointmentDate,

    d.DoctorName,
    d.DoctorImage,

    dep.DepartmentName

FROM tbl_Prescription p

INNER JOIN tbl_Appointment a
ON p.AppointmentId = a.AppointmentId

INNER JOIN tbl_Doctor d
ON p.DoctorId = d.DoctorId

INNER JOIN tbl_Department dep
ON d.DepartmentId = dep.DepartmentId

WHERE

p.CustomerId=@CustomerId
AND p.IsDeleted=0

AND
(
    @Status=''
    OR
    p.PrescriptionStatus=@Status
)

AND
(
    a.AppointmentNo LIKE @Search
    OR
    d.DoctorName LIKE @Search
    OR
    p.Diagnosis LIKE @Search
)

ORDER BY

p.CreatedDate DESC";

                SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@CustomerId", customerId);
                cmd.Parameters.AddWithValue("@Search", "%" + search + "%");
                cmd.Parameters.AddWithValue("@Status", status ?? "");

                SqlDataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    PrescriptionModel model = new PrescriptionModel();

                    model.PrescriptionId = Convert.ToInt64(dr["PrescriptionId"]);

                    model.AppointmentId = Convert.ToInt64(dr["AppointmentId"]);

                    model.AppointmentNo = dr["AppointmentNo"].ToString();

                    if (dr["AppointmentDate"] != DBNull.Value)
                        model.AppointmentDate = Convert.ToDateTime(dr["AppointmentDate"]);

                    model.DoctorName = dr["DoctorName"].ToString();

                    model.DoctorImage = dr["DoctorImage"].ToString();

                    model.DepartmentName = dr["DepartmentName"].ToString();

                    model.Diagnosis = dr["Diagnosis"].ToString();

                    model.Medicines = dr["Medicines"].ToString();

                    if (dr["NextVisitDate"] != DBNull.Value)
                        model.NextVisitDate = Convert.ToDateTime(dr["NextVisitDate"]);

                    model.PrescriptionStatus = dr["PrescriptionStatus"].ToString();

                    model.CreatedDate = Convert.ToDateTime(dr["CreatedDate"]);

                    list.Add(model);
                }

                dr.Close();
            }

            ViewBag.Search = search;
            ViewBag.Status = status;

            ViewBag.TotalPrescription = list.Count;

            return View(list);
        }
    
        [HttpGet]
        public IActionResult ViewPrescription(long id)
        {
         
            if (HttpContext.Session.GetString("CustomerId") == null)
            {
                return RedirectToAction("Login");
            }

            long customerId = Convert.ToInt64(HttpContext.Session.GetString("CustomerId"));

            PrescriptionModel model = new PrescriptionModel();

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

                string query = @"

SELECT

    p.*,

    a.AppointmentNo,
    a.AppointmentDate,
    a.AppointmentTime,

    d.DoctorName,
    d.DoctorImage,
    d.Qualification,
    d.Specialization,
    d.Experience,

    dep.DepartmentName,

    c.FullName,
    c.MobileNo,
    c.Email

FROM tbl_Prescription p

INNER JOIN tbl_Appointment a
ON p.AppointmentId = a.AppointmentId

INNER JOIN tbl_Doctor d
ON p.DoctorId = d.DoctorId

INNER JOIN tbl_Department dep
ON d.DepartmentId = dep.DepartmentId

INNER JOIN tbl_Customer c
ON p.CustomerId = c.CustomerId

WHERE

p.PrescriptionId=@PrescriptionId
AND p.CustomerId=@CustomerId
AND p.IsDeleted=0";

                SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@PrescriptionId", id);
                cmd.Parameters.AddWithValue("@CustomerId", customerId);

                SqlDataReader dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    model.PrescriptionId = Convert.ToInt64(dr["PrescriptionId"]);

                    model.AppointmentId = Convert.ToInt64(dr["AppointmentId"]);

                    model.AppointmentNo = dr["AppointmentNo"].ToString();

                    if (dr["AppointmentDate"] != DBNull.Value)
                        model.AppointmentDate = Convert.ToDateTime(dr["AppointmentDate"]);

                    if (dr["AppointmentTime"] != DBNull.Value)
                        model.AppointmentTime = (TimeSpan?)dr["AppointmentTime"];

                    model.CustomerName = dr["FullName"].ToString();
                    model.MobileNo = dr["MobileNo"].ToString();
                    model.Email = dr["Email"].ToString();

                    model.DoctorName = dr["DoctorName"].ToString();
                    model.DoctorImage = dr["DoctorImage"].ToString();
                    model.DepartmentName = dr["DepartmentName"].ToString();
                    model.Qualification = dr["Qualification"].ToString();
                    model.Specialization = dr["Specialization"].ToString();
                    model.Experience = dr["Experience"].ToString();

                    model.Diagnosis = dr["Diagnosis"].ToString();
                    model.Symptoms = dr["Symptoms"].ToString();
                    model.Medicines = dr["Medicines"].ToString();
                    model.Dosage = dr["Dosage"].ToString();
                    model.Instructions = dr["Instructions"].ToString();

                    if (dr["NextVisitDate"] != DBNull.Value)
                        model.NextVisitDate = Convert.ToDateTime(dr["NextVisitDate"]);

                    model.PrescriptionStatus = dr["PrescriptionStatus"].ToString();

                    model.CreatedDate = Convert.ToDateTime(dr["CreatedDate"]);

                    if (dr["UpdatedDate"] != DBNull.Value)
                        model.UpdatedDate = Convert.ToDateTime(dr["UpdatedDate"]);
                }

                dr.Close();
            }

            if (model.PrescriptionId == 0)
            {
                TempData["Error"] = "Prescription not found.";

                return RedirectToAction("MyPrescriptions");
            }

            return View(model);
        }
       
        [HttpGet]
        public IActionResult DownloadPrescriptionPDF(long id)
        {
            if (HttpContext.Session.GetString("CustomerId") == null)
            {
                return RedirectToAction("Login");
            }

            long customerId = Convert.ToInt64(HttpContext.Session.GetString("CustomerId"));

            PrescriptionModel model = new PrescriptionModel();

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

                string query = @"

SELECT

    p.*,

    a.AppointmentNo,
    a.AppointmentDate,
    a.AppointmentTime,

    c.FullName,
    c.MobileNo,
    c.Email,

    d.DoctorName,
    d.Qualification,
    d.Specialization,

    dep.DepartmentName

FROM tbl_Prescription p

INNER JOIN tbl_Appointment a
ON p.AppointmentId = a.AppointmentId

INNER JOIN tbl_Customer c
ON p.CustomerId = c.CustomerId

INNER JOIN tbl_Doctor d
ON p.DoctorId = d.DoctorId

INNER JOIN tbl_Department dep
ON d.DepartmentId = dep.DepartmentId

WHERE

p.PrescriptionId=@PrescriptionId
AND p.CustomerId=@CustomerId
AND p.IsDeleted=0";

                SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@PrescriptionId", id);
                cmd.Parameters.AddWithValue("@CustomerId", customerId);

                SqlDataReader dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    model.PrescriptionId = Convert.ToInt64(dr["PrescriptionId"]);

                    model.AppointmentNo = dr["AppointmentNo"].ToString();

                    if (dr["AppointmentDate"] != DBNull.Value)
                        model.AppointmentDate = Convert.ToDateTime(dr["AppointmentDate"]);

                    if (dr["AppointmentTime"] != DBNull.Value)
                        model.AppointmentTime = (TimeSpan?)dr["AppointmentTime"];

                    model.CustomerName = dr["FullName"].ToString();

                    model.MobileNo = dr["MobileNo"].ToString();

                    model.Email = dr["Email"].ToString();

                    model.DoctorName = dr["DoctorName"].ToString();

                    model.DepartmentName = dr["DepartmentName"].ToString();

                    model.Qualification = dr["Qualification"].ToString();

                    model.Specialization = dr["Specialization"].ToString();

                    model.Diagnosis = dr["Diagnosis"].ToString();

                    model.Symptoms = dr["Symptoms"].ToString();

                    model.Medicines = dr["Medicines"].ToString();

                    model.Dosage = dr["Dosage"].ToString();

                    model.Instructions = dr["Instructions"].ToString();

                    model.PrescriptionStatus = dr["PrescriptionStatus"].ToString();

                    if (dr["NextVisitDate"] != DBNull.Value)
                        model.NextVisitDate = Convert.ToDateTime(dr["NextVisitDate"]);
                }

                dr.Close();
            }

            if (model.PrescriptionId == 0)
            {
                TempData["Error"] = "Prescription not found.";

                return RedirectToAction("MyPrescriptions");
            }

            byte[] pdf = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);

                    page.Margin(30);

                    page.DefaultTextStyle(x => x.FontSize(11));


                    page.Header()
                        .Column(col =>
                        {
                            col.Spacing(5);

                            col.Item()
                                .AlignCenter()
                                .Text("CLINIC MANAGEMENT SYSTEM")
                                .FontSize(24)
                                .Bold()
                                .FontColor(Colors.Blue.Darken2);

                            col.Item()
                                .AlignCenter()
                                .Text("Patient Prescription")
                                .FontSize(18)
                                .Bold();

                            col.Item()
                                .AlignCenter()
                                .Text("123, Health Street, Ahmedabad, Gujarat")
                                .FontSize(10);

                            col.Item()
                                .AlignCenter()
                                .Text("Phone : +91 9876543210 | Email : clinic@gmail.com")
                                .FontSize(10);

                            col.Item().PaddingTop(8);

                            col.Item().LineHorizontal(1);

                        });


                    page.Content()
                        .PaddingVertical(15)
                        .Column(column =>
                        {
                            column.Spacing(12);

                            column.Item()
                                .AlignCenter()
                                .Text("PRESCRIPTION")
                                .Bold()
                                .FontSize(20)
                                .FontColor(Colors.Green.Darken2);


                            column.Item()
                                .Border(1)
                                .Padding(10)
                                .Column(info =>
                                {
                                    info.Item()
                                        .Text("Appointment Information")
                                        .Bold()
                                        .FontSize(14);

                                    info.Item()
                                        .Text($"Appointment No : {model.AppointmentNo}");

                                    info.Item()
                                        .Text($"Appointment Date : {model.AppointmentDate:dd MMM yyyy}");

                                    info.Item()
                                        .Text($"Appointment Time : {model.AppointmentTime}");
                                });


                            column.Item()
                                .Border(1)
                                .Padding(10)
                                .Column(info =>
                                {
                                    info.Item()
                                        .Text("Patient Information")
                                        .Bold()
                                        .FontSize(14);

                                    info.Item()
                                        .Text($"Patient Name : {model.CustomerName}");

                                    info.Item()
                                        .Text($"Mobile Number : {model.MobileNo}");

                                    info.Item()
                                        .Text($"Email : {model.Email}");
                                });


                            column.Item()
                                .Border(1)
                                .Padding(10)
                                .Column(info =>
                                {
                                    info.Item()
                                        .Text("Doctor Information")
                                        .Bold()
                                        .FontSize(14);

                                    info.Item()
                                        .Text($"Doctor : Dr. {model.DoctorName}");

                                    info.Item()
                                        .Text($"Department : {model.DepartmentName}");

                                    info.Item()
                                        .Text($"Qualification : {model.Qualification}");

                                    info.Item()
                                        .Text($"Specialization : {model.Specialization}");
                                });

                        
                            column.Item()
    .Border(1)
    .Padding(12)
    .Column(info =>
    {
        info.Spacing(10);

        info.Item()
            .Text("Prescription Details")
            .Bold()
            .FontSize(15)
            .FontColor(Colors.Red.Darken2);

        info.Item()
            .Text("Diagnosis")
            .Bold();

        info.Item()
            .Text(string.IsNullOrWhiteSpace(model.Diagnosis)
                ? "-"
                : model.Diagnosis);

        info.Item().LineHorizontal(0.5f);

        info.Item()
            .Text("Symptoms")
            .Bold();

        info.Item()
            .Text(string.IsNullOrWhiteSpace(model.Symptoms)
                ? "-"
                : model.Symptoms);

        info.Item().LineHorizontal(0.5f);

       
        info.Item()
            .Text("Medicines")
            .Bold();

        info.Item()
            .Background(Colors.Grey.Lighten4)
            .Padding(8)
            .Text(string.IsNullOrWhiteSpace(model.Medicines)
                ? "-"
                : model.Medicines);

        info.Item()
            .PaddingTop(8);

        info.Item()
            .Text("Dosage")
            .Bold();

        info.Item()
            .Background(Colors.Grey.Lighten4)
            .Padding(8)
            .Text(string.IsNullOrWhiteSpace(model.Dosage)
                ? "-"
                : model.Dosage);

       
        info.Item()
            .PaddingTop(8);

        info.Item()
            .Text("Doctor Instructions")
            .Bold();

        info.Item()
            .Background(Colors.Grey.Lighten4)
            .Padding(8)
            .Text(string.IsNullOrWhiteSpace(model.Instructions)
                ? "-"
                : model.Instructions);

        info.Item()
            .PaddingTop(10);

        info.Item()
            .Row(row =>
            {
                row.RelativeItem()
                    .Text(txt =>
                    {
                        txt.Span("Next Visit : ").Bold();

                        txt.Span(model.NextVisitDate.HasValue
                            ? model.NextVisitDate.Value.ToString("dd MMM yyyy")
                            : "-");
                    });

                row.RelativeItem()
                    .AlignRight()
                    .Text(txt =>
                    {
                        txt.Span("Status : ").Bold();

                        txt.Span(model.PrescriptionStatus);
                    });
            });
    });

                            column.Item()
                                .PaddingTop(15)
                                .Border(1)
                                .BorderColor(Colors.Orange.Lighten2)
                                .Background(Colors.Orange.Lighten5)
                                .Padding(10)
                                .Column(note =>
                                {
                                    note.Item()
                                        .Text("Important Instructions")
                                        .Bold()
                                        .FontSize(14)
                                        .FontColor(Colors.Orange.Darken2);

                                    note.Item()
                                        .Text("� Take medicines exactly as prescribed by your doctor.");

                                    note.Item()
                                        .Text("� Do not skip doses or stop medicines without consulting the doctor.");

                                    note.Item()
                                        .Text("� Drink plenty of water and maintain a healthy diet.");

                                    note.Item()
                                        .Text("� Visit the clinic immediately if symptoms worsen.");

                                    note.Item()
                                        .Text("� Carry this prescription during your next visit.");
                                });


                            column.Item()
                                .PaddingTop(30);

                            column.Item()
                                .AlignRight()
                                .Column(sign =>
                                {
                                    sign.Item()
                                        .Text("------------------------------");

                                    sign.Item()
                                        .AlignCenter()
                                        .Text("Doctor Signature")
                                        .Bold();

                                    sign.Item()
                                        .AlignCenter()
                                        .Text($"Dr. {model.DoctorName}");

                                    sign.Item()
                                        .AlignCenter()
                                        .Text(model.Qualification ?? "");

                                    sign.Item()
                                        .AlignCenter()
                                        .Text(model.Specialization ?? "");
                                });
                           

                            column.Item()
                                .PaddingTop(20)
                                .AlignCenter()
                                .Text("Get Well Soon!")
                                .Bold()
                                .FontSize(18)
                                .FontColor(Colors.Green.Darken2);

                            column.Item()
                                .AlignCenter()
                                .Text("Follow your doctor's advice and complete the prescribed medicines.")
                                .FontSize(10);

                        });


                    page.Footer()
                        .PaddingTop(10)
                        .Column(col =>
                        {
                            col.Item().LineHorizontal(1);

                            col.Item().PaddingTop(5);

                            col.Item()
                                .AlignCenter()
                                .Text(txt =>
                                {
                                    txt.Span("Generated On : ").Bold();

                                    txt.Span(DateTime.Now.ToString("dd MMM yyyy hh:mm tt"));
                                });

                            col.Item()
                                .AlignCenter()
                                .Text("This is a computer generated prescription.")
                                .FontSize(9)
                                .FontColor(Colors.Grey.Darken1);

                            col.Item()
                                .AlignCenter()
                                .Text(text =>
                                {
                                    text.Span("Page ");

                                    text.CurrentPageNumber();

                                    text.Span(" of ");

                                    text.TotalPages();
                                });

                        });

                });

            }).GeneratePdf();

            string fileName = "Prescription_" + model.AppointmentNo + ".pdf";

            return File(
                pdf,
                "application/pdf",
                fileName);
        }
        //======================================================
        // My Bills
        //======================================================
        [HttpGet]
        public IActionResult MyBills(string search = "", string paymentStatus = "")
        {
            if (HttpContext.Session.GetString("CustomerId") == null)
            {
                return RedirectToAction("Login");
            }

            long customerId = Convert.ToInt64(HttpContext.Session.GetString("CustomerId"));

            List<BillingModel> list = new List<BillingModel>();

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

                string query = @"

SELECT

b.*,

a.AppointmentNo,

d.DoctorName,

dep.DepartmentName

FROM tbl_Billing b

INNER JOIN tbl_Appointment a
ON b.AppointmentId = a.AppointmentId

INNER JOIN tbl_Doctor d
ON b.DoctorId = d.DoctorId

INNER JOIN tbl_Department dep
ON d.DepartmentId = dep.DepartmentId

WHERE

b.CustomerId=@CustomerId
AND b.IsDeleted=0

AND
(
    @PaymentStatus=''
    OR
    b.PaymentStatus=@PaymentStatus
)

AND
(
    b.BillNo LIKE @Search
    OR
    d.DoctorName LIKE @Search
)

ORDER BY b.BillDate DESC";

                SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@CustomerId", customerId);
                cmd.Parameters.AddWithValue("@Search", "%" + search + "%");
                cmd.Parameters.AddWithValue("@PaymentStatus", paymentStatus ?? "");

                SqlDataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    BillingModel model = new BillingModel();

                    model.BillingId = Convert.ToInt64(dr["BillingId"]);
                    model.BillNo = dr["BillNo"].ToString();

                    model.AppointmentId = Convert.ToInt64(dr["AppointmentId"]);

                    model.AppointmentNo = dr["AppointmentNo"].ToString();

                    model.DoctorId = Convert.ToInt64(dr["DoctorId"]);
                    model.DoctorName = dr["DoctorName"].ToString();

                    model.DepartmentName = dr["DepartmentName"].ToString();

                    model.ConsultationFee = Convert.ToDecimal(dr["ConsultationFee"]);
                    model.MedicineCharge = Convert.ToDecimal(dr["MedicineCharge"]);
                    model.LabCharge = Convert.ToDecimal(dr["LabCharge"]);
                    model.OtherCharge = Convert.ToDecimal(dr["OtherCharge"]);

                    model.Discount = Convert.ToDecimal(dr["Discount"]);

                    model.GSTPercentage = Convert.ToDecimal(dr["GSTPercentage"]);
                    model.GSTAmount = Convert.ToDecimal(dr["GSTAmount"]);

                    model.TotalAmount = Convert.ToDecimal(dr["TotalAmount"]);

                    model.PaymentStatus = dr["PaymentStatus"].ToString();
                    model.BillStatus = dr["BillStatus"].ToString();

                    model.BillDate = Convert.ToDateTime(dr["BillDate"]);

                    list.Add(model);
                }

                dr.Close();
            }

            ViewBag.Search = search;
            ViewBag.PaymentStatus = paymentStatus;

            ViewBag.TotalBills = list.Count;

            ViewBag.PaidBills = list.Count(x => x.PaymentStatus == "Paid");

            ViewBag.PendingBills = list.Count(x => x.PaymentStatus == "Pending");

            ViewBag.TotalAmount = list.Sum(x => x.TotalAmount);

            return View(list);
        }
      
        [HttpGet]
        public IActionResult ViewBill(long id)
        {
            if (HttpContext.Session.GetString("CustomerId") == null)
            {
                return RedirectToAction("Login");
            }

            long customerId = Convert.ToInt64(HttpContext.Session.GetString("CustomerId"));

            BillingModel model = new BillingModel();

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

                string query = @"

SELECT

b.*,

a.AppointmentNo,
a.AppointmentDate,
a.AppointmentTime,

d.DoctorName,
d.DoctorImage,

dep.DepartmentName,

c.FullName,
c.MobileNo,
c.Email

FROM tbl_Billing b

INNER JOIN tbl_Appointment a
ON b.AppointmentId = a.AppointmentId

INNER JOIN tbl_Doctor d
ON b.DoctorId = d.DoctorId

INNER JOIN tbl_Department dep
ON d.DepartmentId = dep.DepartmentId

INNER JOIN tbl_Customer c
ON b.CustomerId = c.CustomerId

WHERE

b.BillingId=@BillingId
AND b.CustomerId=@CustomerId
AND b.IsDeleted=0";

                SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@BillingId", id);
                cmd.Parameters.AddWithValue("@CustomerId", customerId);

                SqlDataReader dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    model.BillingId = Convert.ToInt64(dr["BillingId"]);

                    model.BillNo = dr["BillNo"].ToString();

                    model.AppointmentId = Convert.ToInt64(dr["AppointmentId"]);

                    model.AppointmentNo = dr["AppointmentNo"].ToString();

                    if (dr["AppointmentDate"] != DBNull.Value)
                        model.AppointmentDate = Convert.ToDateTime(dr["AppointmentDate"]);

                    if (dr["AppointmentTime"] != DBNull.Value)
                        model.AppointmentTime = (TimeSpan?)dr["AppointmentTime"];

                    model.CustomerId = Convert.ToInt64(dr["CustomerId"]);

                    model.CustomerName = dr["FullName"].ToString();

                    model.MobileNo = dr["MobileNo"].ToString();

                    model.Email = dr["Email"].ToString();

                    model.DoctorId = Convert.ToInt64(dr["DoctorId"]);

                    model.DoctorName = dr["DoctorName"].ToString();

                    model.DoctorImage = dr["DoctorImage"].ToString();

                    model.DepartmentName = dr["DepartmentName"].ToString();

                    model.ConsultationFee = Convert.ToDecimal(dr["ConsultationFee"]);

                    model.MedicineCharge = Convert.ToDecimal(dr["MedicineCharge"]);

                    model.LabCharge = Convert.ToDecimal(dr["LabCharge"]);

                    model.OtherCharge = Convert.ToDecimal(dr["OtherCharge"]);

                    model.Discount = Convert.ToDecimal(dr["Discount"]);

                    model.GSTPercentage = Convert.ToDecimal(dr["GSTPercentage"]);

                    model.GSTAmount = Convert.ToDecimal(dr["GSTAmount"]);

                    model.TotalAmount = Convert.ToDecimal(dr["TotalAmount"]);

                    model.PaymentStatus = dr["PaymentStatus"].ToString();

                    model.BillStatus = dr["BillStatus"].ToString();

                    model.Remarks = dr["Remarks"] == DBNull.Value
                        ? ""
                        : dr["Remarks"].ToString();

                    model.BillDate = Convert.ToDateTime(dr["BillDate"]);

                    model.CreatedDate = Convert.ToDateTime(dr["CreatedDate"]);

                    if (dr["UpdatedDate"] != DBNull.Value)
                    {
                        model.UpdatedDate = Convert.ToDateTime(dr["UpdatedDate"]);
                    }
                }

                dr.Close();
            }

            if (model.BillingId == 0)
            {
                TempData["Error"] = "Bill not found.";

                return RedirectToAction("MyBills");
            }

            return View(model);
        }
      
        [HttpGet]
        public IActionResult DownloadBillPDF(long id)
        {
            if (HttpContext.Session.GetString("CustomerId") == null)
            {
                return RedirectToAction("Login");
            }

            long customerId = Convert.ToInt64(HttpContext.Session.GetString("CustomerId"));

            BillingModel model = new BillingModel();

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

                string query = @"

SELECT

b.*,

a.AppointmentNo,
a.AppointmentDate,
a.AppointmentTime,

c.FullName,
c.MobileNo,
c.Email,

d.DoctorName,
d.DoctorImage,

dep.DepartmentName

FROM tbl_Billing b

INNER JOIN tbl_Appointment a
ON b.AppointmentId = a.AppointmentId

INNER JOIN tbl_Customer c
ON b.CustomerId = c.CustomerId

INNER JOIN tbl_Doctor d
ON b.DoctorId = d.DoctorId

INNER JOIN tbl_Department dep
ON d.DepartmentId = dep.DepartmentId

WHERE

b.BillingId=@BillingId
AND b.CustomerId=@CustomerId
AND b.IsDeleted=0";

                SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@BillingId", id);
                cmd.Parameters.AddWithValue("@CustomerId", customerId);

                SqlDataReader dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    model.BillingId = Convert.ToInt64(dr["BillingId"]);

                    model.BillNo = dr["BillNo"].ToString();

                    model.AppointmentNo = dr["AppointmentNo"].ToString();

                    if (dr["AppointmentDate"] != DBNull.Value)
                        model.AppointmentDate = Convert.ToDateTime(dr["AppointmentDate"]);

                    if (dr["AppointmentTime"] != DBNull.Value)
                        model.AppointmentTime = (TimeSpan?)dr["AppointmentTime"];

                    model.CustomerName = dr["FullName"].ToString();

                    model.MobileNo = dr["MobileNo"].ToString();

                    model.Email = dr["Email"].ToString();

                    model.DoctorName = dr["DoctorName"].ToString();

                    model.DoctorImage = dr["DoctorImage"].ToString();

                    model.DepartmentName = dr["DepartmentName"].ToString();

                    model.ConsultationFee = Convert.ToDecimal(dr["ConsultationFee"]);

                    model.MedicineCharge = Convert.ToDecimal(dr["MedicineCharge"]);

                    model.LabCharge = Convert.ToDecimal(dr["LabCharge"]);

                    model.OtherCharge = Convert.ToDecimal(dr["OtherCharge"]);

                    model.Discount = Convert.ToDecimal(dr["Discount"]);

                    model.GSTPercentage = Convert.ToDecimal(dr["GSTPercentage"]);

                    model.GSTAmount = Convert.ToDecimal(dr["GSTAmount"]);

                    model.TotalAmount = Convert.ToDecimal(dr["TotalAmount"]);

                    model.PaymentStatus = dr["PaymentStatus"].ToString();

                    model.BillStatus = dr["BillStatus"].ToString();

                    model.Remarks = dr["Remarks"] == DBNull.Value
                        ? ""
                        : dr["Remarks"].ToString();

                    model.BillDate = Convert.ToDateTime(dr["BillDate"]);
                }

                dr.Close();
            }

            if (model.BillingId == 0)
            {
                TempData["Error"] = "Bill not found.";

                return RedirectToAction("MyBills");
            }

            byte[] pdf = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);

                    page.Margin(30);

                    page.DefaultTextStyle(x => x.FontSize(11));


                    page.Header()
                        .Column(col =>
                        {
                            col.Spacing(5);

                            col.Item()
                                .AlignCenter()
                                .Text("CLINIC MANAGEMENT SYSTEM")
                                .FontSize(24)
                                .Bold()
                                .FontColor(Colors.Blue.Darken2);

                            col.Item()
                                .AlignCenter()
                                .Text("MEDICAL BILL / INVOICE")
                                .FontSize(18)
                                .Bold();

                            col.Item()
                                .AlignCenter()
                                .Text("123 Health Street, Ahmedabad, Gujarat")
                                .FontSize(10);

                            col.Item()
                                .AlignCenter()
                                .Text("Phone : +91 9876543210 | Email : clinic@gmail.com")
                                .FontSize(10);

                            col.Item()
                                .PaddingTop(8);

                            col.Item()
                                .LineHorizontal(1);

                        });


                    page.Content()
                        .PaddingVertical(15)
                        .Column(column =>
                        {
                            column.Spacing(15);

                            column.Item()
                                .AlignCenter()
                                .Text("PATIENT BILL")
                                .Bold()
                                .FontSize(20)
                                .FontColor(Colors.Red.Darken2);


                            column.Item()
                                .Border(1)
                                .Padding(10)
                                .Column(info =>
                                {
                                    info.Item()
                                        .Text("Bill Information")
                                        .Bold()
                                        .FontSize(14);

                                    info.Item()
                                        .Text($"Bill Number : {model.BillNo}");

                                    info.Item()
                                        .Text($"Bill Date : {model.BillDate:dd MMM yyyy}");

                                    info.Item()
                                        .Text($"Payment Status : {model.PaymentStatus}");

                                    info.Item()
                                        .Text($"Bill Status : {model.BillStatus}");
                                });


                            column.Item()
                                .Border(1)
                                .Padding(10)
                                .Column(info =>
                                {
                                    info.Item()
                                        .Text("Patient Information")
                                        .Bold()
                                        .FontSize(14);

                                    info.Item()
                                        .Text($"Patient Name : {model.CustomerName}");

                                    info.Item()
                                        .Text($"Mobile Number : {model.MobileNo}");

                                    info.Item()
                                        .Text($"Email Address : {model.Email}");
                                });


                            column.Item()
                                .Border(1)
                                .Padding(10)
                                .Column(info =>
                                {
                                    info.Item()
                                        .Text("Doctor Information")
                                        .Bold()
                                        .FontSize(14);

                                    info.Item()
                                        .Text($"Doctor : Dr. {model.DoctorName}");

                                    info.Item()
                                        .Text($"Department : {model.DepartmentName}");
                                });


                            column.Item()
                                .Border(1)
                                .Padding(10)
                                .Column(info =>
                                {
                                    info.Item()
                                        .Text("Appointment Information")
                                        .Bold()
                                        .FontSize(14);

                                    info.Item()
                                        .Text($"Appointment No : {model.AppointmentNo}");

                                    info.Item()
                                        .Text($"Appointment Date : {model.AppointmentDate:dd MMM yyyy}");

                                    info.Item()
                                        .Text($"Appointment Time : {model.AppointmentTime}");
                                });

                            column.Item()
    .Border(1)
    .Padding(10)
    .Column(info =>
    {
        info.Item()
            .Text("Charges Details")
            .Bold()
            .FontSize(15)
            .FontColor(Colors.Green.Darken2);

        info.Item().PaddingTop(10);

        info.Item().Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(3);
                columns.RelativeColumn(1);
            });


            table.Header(header =>
            {
                header.Cell()
                    .Background(Colors.Grey.Lighten2)
                    .Padding(8)
                    .Text("Particular")
                    .Bold();

                header.Cell()
                    .Background(Colors.Grey.Lighten2)
                    .Padding(8)
                    .AlignRight()
                    .Text("Amount")
                    .Bold();
            });

  

            table.Cell()
                .BorderBottom(1)
                .Padding(8)
                .Text("Consultation Fee");

            table.Cell()
                .BorderBottom(1)
                .Padding(8)
                .AlignRight()
                .Text($"? {model.ConsultationFee:0.00}");


            table.Cell()
                .BorderBottom(1)
                .Padding(8)
                .Text("Medicine Charge");

            table.Cell()
                .BorderBottom(1)
                .Padding(8)
                .AlignRight()
                .Text($"? {model.MedicineCharge:0.00}");


            table.Cell()
                .BorderBottom(1)
                .Padding(8)
                .Text("Lab Charge");

            table.Cell()
                .BorderBottom(1)
                .Padding(8)
                .AlignRight()
                .Text($"? {model.LabCharge:0.00}");


            table.Cell()
                .BorderBottom(1)
                .Padding(8)
                .Text("Other Charge");

            table.Cell()
                .BorderBottom(1)
                .Padding(8)
                .AlignRight()
                .Text($"? {model.OtherCharge:0.00}");


            table.Cell()
                .BorderBottom(1)
                .Padding(8)
                .Text("Discount");

            table.Cell()
                .BorderBottom(1)
                .Padding(8)
                .AlignRight()
                .Text($"- ? {model.Discount:0.00}");

           

            table.Cell()
                .BorderBottom(1)
                .Padding(8)
                .Text($"GST ({model.GSTPercentage:0.##}%)");

            table.Cell()
                .BorderBottom(1)
                .Padding(8)
                .AlignRight()
                .Text($"? {model.GSTAmount:0.00}");
        });


        info.Item()
            .PaddingTop(15);

        info.Item()
            .Background(Colors.Green.Lighten4)
            .Border(1)
            .BorderColor(Colors.Green.Darken1)
            .Padding(12)
            .Row(row =>
            {
                row.RelativeItem()
                    .Text("Grand Total")
                    .Bold()
                    .FontSize(16);

                row.RelativeItem()
                    .AlignRight()
                    .Text($"? {model.TotalAmount:0.00}")
                    .Bold()
                    .FontSize(18)
                    .FontColor(Colors.Green.Darken3);
            });


        if (!string.IsNullOrWhiteSpace(model.Remarks))
        {
            info.Item()
                .PaddingTop(15);

            info.Item()
                .Text("Remarks")
                .Bold()
                .FontSize(13);

            info.Item()
                .Border(1)
                .Padding(8)
                .Text(model.Remarks);
        }
    });


                            column.Item()
                                .PaddingTop(20)
                                .Border(1)
                                .BorderColor(Colors.Blue.Lighten2)
                                .Background(Colors.Blue.Lighten5)
                                .Padding(10)
                                .Column(note =>
                                {
                                    note.Item()
                                        .Text("Important Notes")
                                        .Bold()
                                        .FontSize(14)
                                        .FontColor(Colors.Blue.Darken2);

                                    note.Item()
                                        .Text("� This invoice is generated by Clinic Management System.");

                                    note.Item()
                                        .Text("� Please keep this bill safely for future medical reference.");

                                    note.Item()
                                        .Text("� Contact the clinic if you have any billing questions.");

                                    note.Item()
                                        .Text("� We appreciate your trust in our healthcare services.");
                                });
                          

                            column.Item()
                                .PaddingTop(30);

                            column.Item()
                                .AlignRight()
                                .Column(signature =>
                                {
                                    signature.Item()
                                        .Text("--------------------------------");

                                    signature.Item()
                                        .AlignCenter()
                                        .Text("Authorized Signature")
                                        .Bold();

                                    signature.Item()
                                        .AlignCenter()
                                        .Text("Clinic Administrator");

                                    signature.Item()
                                        .AlignCenter()
                                        .Text("Clinic Management System");
                                });

                        

                            column.Item()
                                .PaddingTop(20)
                                .AlignCenter()
                                .Text("Thank You For Choosing Our Clinic")
                                .Bold()
                                .FontSize(18)
                                .FontColor(Colors.Green.Darken2);

                            column.Item()
                                .AlignCenter()
                                .Text("We wish you good health and a speedy recovery.")
                                .FontSize(10);

                        });


                    page.Footer()
                        .PaddingTop(10)
                        .Column(col =>
                        {
                            col.Item().LineHorizontal(1);

                            col.Item()
                                .PaddingTop(5)
                                .AlignCenter()
                                .Text(text =>
                                {
                                    text.Span("Generated On : ").Bold();
                                    text.Span(DateTime.Now.ToString("dd MMM yyyy hh:mm tt"));
                                });

                            col.Item()
                                .AlignCenter()
                                .Text("This is a computer generated medical invoice.")
                                .FontSize(9)
                                .FontColor(Colors.Grey.Darken1);

                            col.Item()
                                .AlignCenter()
                                .Text(text =>
                                {
                                    text.Span("Page ");
                                    text.CurrentPageNumber();
                                    text.Span(" of ");
                                    text.TotalPages();
                                });
                        });

                });

            }).GeneratePdf();

            string fileName = $"MedicalBill_{model.BillNo}.pdf";

            return File(
                pdf,
                "application/pdf",
                fileName);
        }
       
        [HttpGet]
        public IActionResult Gallery(string search = "", string category = "")
        {
            if (HttpContext.Session.GetString("CustomerId") == null)
            {
                return RedirectToAction("Login");
            }

            List<GalleryModel> list = new List<GalleryModel>();

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

                string query = @"

SELECT *

FROM tbl_Gallery

WHERE

IsDeleted = 0
AND IsActive = 1

AND
(
    @Search = ''
    OR
    GalleryTitle LIKE @SearchText
)

AND
(
    @Category = ''
    OR
    GalleryCategory = @Category
)

ORDER BY

DisplayOrder ASC,
GalleryId DESC";

                SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@Search", search ?? "");

                cmd.Parameters.AddWithValue("@SearchText", "%" + (search ?? "") + "%");

                cmd.Parameters.AddWithValue("@Category", category ?? "");

                SqlDataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    GalleryModel model = new GalleryModel();

                    model.GalleryId = Convert.ToInt64(dr["GalleryId"]);

                    model.GalleryTitle = dr["GalleryTitle"].ToString();

                    model.GalleryCategory = dr["GalleryCategory"].ToString();

                    model.GalleryImage = dr["GalleryImage"].ToString();

                    model.Description = dr["Description"] == DBNull.Value
                        ? ""
                        : dr["Description"].ToString();

                    model.DisplayOrder = Convert.ToInt32(dr["DisplayOrder"]);

                    model.IsActive = Convert.ToBoolean(dr["IsActive"]);

                    model.CreatedDate = Convert.ToDateTime(dr["CreatedDate"]);

                    if (dr["UpdatedDate"] != DBNull.Value)
                    {
                        model.UpdatedDate = Convert.ToDateTime(dr["UpdatedDate"]);
                    }

                    list.Add(model);
                }

                dr.Close();
            }

            ViewBag.Search = search;
            ViewBag.Category = category;

            return View(list);
        }
     
        [HttpGet]
        public IActionResult GalleryDetails(long id)
        {
          
            if (HttpContext.Session.GetString("CustomerId") == null)
            {
                return RedirectToAction("Login");
            }

            GalleryModel model = new GalleryModel();

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

                string query = @"

SELECT *

FROM tbl_Gallery

WHERE

GalleryId = @GalleryId
AND IsActive = 1
AND IsDeleted = 0";

                SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@GalleryId", id);

                SqlDataReader dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    model.GalleryId = Convert.ToInt64(dr["GalleryId"]);

                    model.GalleryTitle = dr["GalleryTitle"].ToString();

                    model.GalleryCategory = dr["GalleryCategory"].ToString();

                    model.GalleryImage = dr["GalleryImage"].ToString();

                    model.Description = dr["Description"] == DBNull.Value
                        ? ""
                        : dr["Description"].ToString();

                    model.DisplayOrder = Convert.ToInt32(dr["DisplayOrder"]);

                    model.IsActive = Convert.ToBoolean(dr["IsActive"]);

                    model.IsDeleted = Convert.ToBoolean(dr["IsDeleted"]);

                    model.CreatedDate = Convert.ToDateTime(dr["CreatedDate"]);

                    if (dr["UpdatedDate"] != DBNull.Value)
                    {
                        model.UpdatedDate = Convert.ToDateTime(dr["UpdatedDate"]);
                    }
                }
                else
                {
                    dr.Close();

                    TempData["Error"] = "Gallery image not found.";

                    return RedirectToAction("Gallery");
                }

                dr.Close();
            }

            return View(model);
        }

     
        [HttpGet]
        public IActionResult AddFeedback(long appointmentId)
        {
            if (HttpContext.Session.GetString("CustomerId") == null)
            {
                return RedirectToAction("Login");
            }

            FeedbackModel model = new FeedbackModel();

            long customerId = Convert.ToInt64(HttpContext.Session.GetString("CustomerId"));

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();


                string checkQuery = @"

SELECT COUNT(*)

FROM tbl_Feedback

WHERE

AppointmentId=@AppointmentId
AND CustomerId=@CustomerId
AND IsDeleted=0";

                SqlCommand checkCmd = new SqlCommand(checkQuery, con);

                checkCmd.Parameters.AddWithValue("@AppointmentId", appointmentId);

                checkCmd.Parameters.AddWithValue("@CustomerId", customerId);

                int count = Convert.ToInt32(checkCmd.ExecuteScalar());

                if (count > 0)
                {
                    TempData["Error"] = "Feedback already submitted for this appointment.";

                    return RedirectToAction("MyFeedback");
                }

           

                string query = @"

SELECT

A.AppointmentId,
A.AppointmentNo,
A.AppointmentDate,
A.AppointmentStatus,

D.DoctorId,
D.DoctorName,
D.Specialization,

P.CustomerId,
P.FullName AS CustomerName

FROM tbl_Appointment A

INNER JOIN tbl_Doctor D
ON A.DoctorId = D.DoctorId

INNER JOIN tbl_Customer P
ON A.CustomerId = P.CustomerId

WHERE

A.AppointmentId = @AppointmentId
AND A.CustomerId = @CustomerId
AND A.AppointmentStatus = 'Completed'";

                SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@AppointmentId", appointmentId);

                cmd.Parameters.AddWithValue("@CustomerId", customerId);

                SqlDataReader dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    model.AppointmentId = Convert.ToInt64(dr["AppointmentId"]);

                    model.AppointmentNo = dr["AppointmentNo"].ToString();

                    model.CustomerId = Convert.ToInt64(dr["CustomerId"]);

                    model.CustomerName = dr["CustomerName"].ToString();

                    model.DoctorId = Convert.ToInt64(dr["DoctorId"]);

                    model.DoctorName = dr["DoctorName"].ToString();

                    model.DoctorSpecialization = dr["Specialization"].ToString();

                    model.AppointmentDate = Convert.ToDateTime(dr["AppointmentDate"]);
                }
                else
                {
                    dr.Close();

                    TempData["Error"] = "Completed appointment not found.";

                    return RedirectToAction("MyAppointments");
                }

                dr.Close();
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddFeedback(FeedbackModel model)
        {
            if (HttpContext.Session.GetString("CustomerId") == null)
            {
                return RedirectToAction("Login");
            }

            ModelState.Remove("CustomerName");
            ModelState.Remove("DoctorName");
            ModelState.Remove("AppointmentNo");
            ModelState.Remove("DepartmentName");
            ModelState.Remove("CustomerImage");
            ModelState.Remove("DoctorImage");
            ModelState.Remove("DoctorSpecialization");
            ModelState.Remove("ReplyMessage");

            if (!ModelState.IsValid)
            {
                return AddFeedback(model.AppointmentId);
            }

            long customerId = Convert.ToInt64(HttpContext.Session.GetString("CustomerId"));

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();


                string checkQuery = @"

SELECT COUNT(*)

FROM tbl_Feedback

WHERE

AppointmentId=@AppointmentId
AND CustomerId=@CustomerId
AND IsDeleted=0";

                SqlCommand checkCmd = new SqlCommand(checkQuery, con);

                checkCmd.Parameters.AddWithValue("@AppointmentId", model.AppointmentId);

                checkCmd.Parameters.AddWithValue("@CustomerId", customerId);

                int count = Convert.ToInt32(checkCmd.ExecuteScalar());

                if (count > 0)
                {
                    TempData["Error"] = "Feedback already submitted.";

                    return RedirectToAction("MyFeedback");
                }


                string insertQuery = @"

INSERT INTO tbl_Feedback
(
    CustomerId,
    AppointmentId,
    DoctorId,
    Rating,
    Subject,
    FeedbackMessage,
    ReplyMessage,
    IsApproved,
    IsActive,
    IsDeleted,
    CreatedDate
)

VALUES
(
    @CustomerId,
    @AppointmentId,
    @DoctorId,
    @Rating,
    @Subject,
    @FeedbackMessage,
    @ReplyMessage,
    @IsApproved,
    @IsActive,
    @IsDeleted,
    @CreatedDate
)";

                SqlCommand cmd = new SqlCommand(insertQuery, con);

                cmd.Parameters.AddWithValue("@CustomerId", customerId);

                cmd.Parameters.AddWithValue("@AppointmentId", model.AppointmentId);

                cmd.Parameters.AddWithValue("@DoctorId", model.DoctorId);

                cmd.Parameters.AddWithValue("@Rating", model.Rating);

                cmd.Parameters.AddWithValue("@Subject", model.Subject);

                cmd.Parameters.AddWithValue("@FeedbackMessage", model.FeedbackMessage);

                cmd.Parameters.AddWithValue("@ReplyMessage", DBNull.Value);

                cmd.Parameters.AddWithValue("@IsApproved", false);

                cmd.Parameters.AddWithValue("@IsActive", true);

                cmd.Parameters.AddWithValue("@IsDeleted", false);

                cmd.Parameters.AddWithValue("@CreatedDate", DateTime.Now);

                int result = cmd.ExecuteNonQuery();

                if (result > 0)
                {
                    TempData["Success"] = "Thank you! Your feedback has been submitted successfully.";

                    return RedirectToAction("MyFeedback");
                }

                TempData["Error"] = "Unable to submit feedback.";
            }

            return RedirectToAction("MyFeedback");
        }

        [HttpGet]
        public IActionResult MyFeedback(string search = "")
        {
            if (HttpContext.Session.GetString("CustomerId") == null)
            {
                return RedirectToAction("Login");
            }

            List<FeedbackModel> list = new List<FeedbackModel>();

            long customerId = Convert.ToInt64(HttpContext.Session.GetString("CustomerId"));

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

                string query = @"

SELECT

F.FeedbackId,
F.CustomerId,
F.AppointmentId,
F.DoctorId,
F.Rating,
F.Subject,
F.FeedbackMessage,
F.ReplyMessage,
F.IsApproved,
F.IsActive,
F.IsDeleted,
F.CreatedDate,
F.UpdatedDate,

A.AppointmentNo,

D.DoctorName,

DP.DepartmentName

FROM tbl_Feedback F

INNER JOIN tbl_Appointment A
ON F.AppointmentId = A.AppointmentId

INNER JOIN tbl_Doctor D
ON F.DoctorId = D.DoctorId

LEFT JOIN tbl_Department DP
ON D.DepartmentId = DP.DepartmentId

WHERE

F.CustomerId = @CustomerId
AND F.IsDeleted = 0

AND
(
    @Search = ''
    OR
    D.DoctorName LIKE @SearchText
    OR
    A.AppointmentNo LIKE @SearchText
    OR
    F.Subject LIKE @SearchText
)

ORDER BY

F.FeedbackId DESC";

                SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@CustomerId", customerId);

                cmd.Parameters.AddWithValue("@Search", search ?? "");

                cmd.Parameters.AddWithValue("@SearchText", "%" + (search ?? "") + "%");

                SqlDataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    FeedbackModel model = new FeedbackModel();

                    model.FeedbackId = Convert.ToInt64(dr["FeedbackId"]);

                    model.CustomerId = Convert.ToInt64(dr["CustomerId"]);

                    model.AppointmentId = Convert.ToInt64(dr["AppointmentId"]);

                    model.DoctorId = Convert.ToInt64(dr["DoctorId"]);

                    model.AppointmentNo = dr["AppointmentNo"].ToString();

                    model.DoctorName = dr["DoctorName"].ToString();

                    model.DepartmentName = dr["DepartmentName"] == DBNull.Value
                        ? ""
                        : dr["DepartmentName"].ToString();

                    model.Rating = Convert.ToInt32(dr["Rating"]);

                    model.Subject = dr["Subject"].ToString();

                    model.FeedbackMessage = dr["FeedbackMessage"].ToString();

                    model.ReplyMessage = dr["ReplyMessage"] == DBNull.Value
                        ? ""
                        : dr["ReplyMessage"].ToString();

                    model.IsApproved = Convert.ToBoolean(dr["IsApproved"]);

                    model.IsActive = Convert.ToBoolean(dr["IsActive"]);

                    model.IsDeleted = Convert.ToBoolean(dr["IsDeleted"]);

                    model.CreatedDate = Convert.ToDateTime(dr["CreatedDate"]);

                    if (dr["UpdatedDate"] != DBNull.Value)
                    {
                        model.UpdatedDate = Convert.ToDateTime(dr["UpdatedDate"]);
                    }

                    list.Add(model);
                }

                dr.Close();
            }

            ViewBag.Search = search;

            return View(list);
        }

      
        [HttpGet]
        public IActionResult ViewFeedback(long id)
        {
            if (HttpContext.Session.GetString("CustomerId") == null)
            {
                return RedirectToAction("Login");
            }

            FeedbackModel model = new FeedbackModel();

            long customerId = Convert.ToInt64(HttpContext.Session.GetString("CustomerId"));

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

                string query = @"

SELECT

F.FeedbackId,
F.CustomerId,
F.AppointmentId,
F.DoctorId,
F.Rating,
F.Subject,
F.FeedbackMessage,
F.ReplyMessage,
F.IsApproved,
F.IsActive,
F.CreatedDate,
F.UpdatedDate,

A.AppointmentNo,
A.AppointmentDate,

D.DoctorName,
D.Specialization,

DP.DepartmentName

FROM tbl_Feedback F

INNER JOIN tbl_Appointment A
ON F.AppointmentId = A.AppointmentId

INNER JOIN tbl_Doctor D
ON F.DoctorId = D.DoctorId

LEFT JOIN tbl_Department DP
ON D.DepartmentId = DP.DepartmentId

WHERE

F.FeedbackId = @FeedbackId
AND F.CustomerId = @CustomerId
AND F.IsDeleted = 0";

                SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@FeedbackId", id);
                cmd.Parameters.AddWithValue("@CustomerId", customerId);

                SqlDataReader dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    model.FeedbackId = Convert.ToInt64(dr["FeedbackId"]);

                    model.CustomerId = Convert.ToInt64(dr["CustomerId"]);

                    model.AppointmentId = Convert.ToInt64(dr["AppointmentId"]);

                    model.DoctorId = Convert.ToInt64(dr["DoctorId"]);

                    model.AppointmentNo = dr["AppointmentNo"].ToString();

                    model.AppointmentDate = Convert.ToDateTime(dr["AppointmentDate"]);

                    model.DoctorName = dr["DoctorName"].ToString();

                    model.DoctorSpecialization = dr["Specialization"].ToString();

                    model.DepartmentName = dr["DepartmentName"] == DBNull.Value
                        ? ""
                        : dr["DepartmentName"].ToString();

                    model.Rating = Convert.ToInt32(dr["Rating"]);

                    model.Subject = dr["Subject"].ToString();

                    model.FeedbackMessage = dr["FeedbackMessage"].ToString();

                    model.ReplyMessage = dr["ReplyMessage"] == DBNull.Value
                        ? ""
                        : dr["ReplyMessage"].ToString();

                    model.IsApproved = Convert.ToBoolean(dr["IsApproved"]);

                    model.IsActive = Convert.ToBoolean(dr["IsActive"]);

                    model.CreatedDate = Convert.ToDateTime(dr["CreatedDate"]);

                    if (dr["UpdatedDate"] != DBNull.Value)
                    {
                        model.UpdatedDate = Convert.ToDateTime(dr["UpdatedDate"]);
                    }
                }
                else
                {
                    dr.Close();

                    TempData["Error"] = "Feedback not found.";

                    return RedirectToAction("MyFeedback");
                }

                dr.Close();
            }

            return View(model);
        }
       
        [HttpGet]
        public IActionResult EditFeedback(long id)
        {
            if (HttpContext.Session.GetString("CustomerId") == null)
            {
                return RedirectToAction("Login");
            }

            FeedbackModel model = new FeedbackModel();

            long customerId = Convert.ToInt64(HttpContext.Session.GetString("CustomerId"));

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

                string query = @"

SELECT

F.FeedbackId,
F.CustomerId,
F.AppointmentId,
F.DoctorId,
F.Rating,
F.Subject,
F.FeedbackMessage,
F.ReplyMessage,
F.IsApproved,
F.IsActive,
F.CreatedDate,
F.UpdatedDate,

A.AppointmentNo,
A.AppointmentDate,

D.DoctorName,
D.Specialization,

DP.DepartmentName

FROM tbl_Feedback F

INNER JOIN tbl_Appointment A
ON F.AppointmentId = A.AppointmentId

INNER JOIN tbl_Doctor D
ON F.DoctorId = D.DoctorId

LEFT JOIN tbl_Department DP
ON D.DepartmentId = DP.DepartmentId

WHERE

F.FeedbackId = @FeedbackId
AND F.CustomerId = @CustomerId
AND F.IsDeleted = 0";

                SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@FeedbackId", id);
                cmd.Parameters.AddWithValue("@CustomerId", customerId);

                SqlDataReader dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    if (dr["ReplyMessage"] != DBNull.Value &&
                        !string.IsNullOrWhiteSpace(dr["ReplyMessage"].ToString()))
                    {
                        dr.Close();

                        TempData["Error"] = "Feedback cannot be edited because admin has already replied.";

                        return RedirectToAction("MyFeedback");
                    }

                    model.FeedbackId = Convert.ToInt64(dr["FeedbackId"]);

                    model.CustomerId = Convert.ToInt64(dr["CustomerId"]);

                    model.AppointmentId = Convert.ToInt64(dr["AppointmentId"]);

                    model.DoctorId = Convert.ToInt64(dr["DoctorId"]);

                    model.AppointmentNo = dr["AppointmentNo"].ToString();

                    model.AppointmentDate = Convert.ToDateTime(dr["AppointmentDate"]);

                    model.DoctorName = dr["DoctorName"].ToString();

                    model.DoctorSpecialization = dr["Specialization"].ToString();

                    model.DepartmentName = dr["DepartmentName"] == DBNull.Value
                        ? ""
                        : dr["DepartmentName"].ToString();

                    model.Rating = Convert.ToInt32(dr["Rating"]);

                    model.Subject = dr["Subject"].ToString();

                    model.FeedbackMessage = dr["FeedbackMessage"].ToString();

                    model.ReplyMessage = dr["ReplyMessage"] == DBNull.Value
                        ? ""
                        : dr["ReplyMessage"].ToString();

                    model.IsApproved = Convert.ToBoolean(dr["IsApproved"]);

                    model.IsActive = Convert.ToBoolean(dr["IsActive"]);

                    model.CreatedDate = Convert.ToDateTime(dr["CreatedDate"]);

                    if (dr["UpdatedDate"] != DBNull.Value)
                    {
                        model.UpdatedDate = Convert.ToDateTime(dr["UpdatedDate"]);
                    }
                }
                else
                {
                    dr.Close();

                    TempData["Error"] = "Feedback not found.";

                    return RedirectToAction("MyFeedback");
                }

                dr.Close();
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditFeedback(FeedbackModel model)
        {
            if (HttpContext.Session.GetString("CustomerId") == null)
            {
                return RedirectToAction("Login");
            }

            ModelState.Remove("CustomerName");
            ModelState.Remove("DoctorName");
            ModelState.Remove("AppointmentNo");
            ModelState.Remove("DepartmentName");
            ModelState.Remove("DoctorSpecialization");
            ModelState.Remove("ReplyMessage");
            ModelState.Remove("CustomerImage");
            ModelState.Remove("DoctorImage");

            if (!ModelState.IsValid)
            {
                return EditFeedback(model.FeedbackId);
            }

            long customerId = Convert.ToInt64(HttpContext.Session.GetString("CustomerId"));

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

                string checkQuery = @"

SELECT ReplyMessage

FROM tbl_Feedback

WHERE

FeedbackId=@FeedbackId
AND CustomerId=@CustomerId
AND IsDeleted=0";

                SqlCommand checkCmd = new SqlCommand(checkQuery, con);

                checkCmd.Parameters.AddWithValue("@FeedbackId", model.FeedbackId);
                checkCmd.Parameters.AddWithValue("@CustomerId", customerId);

                object reply = checkCmd.ExecuteScalar();

                if (reply == null)
                {
                    TempData["Error"] = "Feedback not found.";

                    return RedirectToAction("MyFeedback");
                }

                if (reply != DBNull.Value &&
                    !string.IsNullOrWhiteSpace(reply.ToString()))
                {
                    TempData["Error"] = "Feedback cannot be edited because admin has already replied.";

                    return RedirectToAction("MyFeedback");
                }

                string updateQuery = @"

UPDATE tbl_Feedback

SET

Rating=@Rating,
Subject=@Subject,
FeedbackMessage=@FeedbackMessage,
UpdatedDate=@UpdatedDate

WHERE

FeedbackId=@FeedbackId
AND CustomerId=@CustomerId
AND IsDeleted=0";

                SqlCommand cmd = new SqlCommand(updateQuery, con);

                cmd.Parameters.AddWithValue("@FeedbackId", model.FeedbackId);
                cmd.Parameters.AddWithValue("@CustomerId", customerId);
                cmd.Parameters.AddWithValue("@Rating", model.Rating);
                cmd.Parameters.AddWithValue("@Subject", model.Subject);
                cmd.Parameters.AddWithValue("@FeedbackMessage", model.FeedbackMessage);
                cmd.Parameters.AddWithValue("@UpdatedDate", DateTime.Now);

                int result = cmd.ExecuteNonQuery();

                if (result > 0)
                {
                    TempData["Success"] = "Feedback updated successfully.";

                    return RedirectToAction("MyFeedback");
                }

                TempData["Error"] = "Unable to update feedback.";
            }

            return RedirectToAction("MyFeedback");
        }

        [HttpGet]
        public IActionResult DeleteFeedback(long id)
        {
            if (HttpContext.Session.GetString("CustomerId") == null)
            {
                return RedirectToAction("Login");
            }

            long customerId = Convert.ToInt64(HttpContext.Session.GetString("CustomerId"));

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();


                string checkQuery = @"

SELECT ReplyMessage

FROM tbl_Feedback

WHERE

FeedbackId = @FeedbackId
AND CustomerId = @CustomerId
AND IsDeleted = 0";

                SqlCommand checkCmd = new SqlCommand(checkQuery, con);

                checkCmd.Parameters.AddWithValue("@FeedbackId", id);

                checkCmd.Parameters.AddWithValue("@CustomerId", customerId);

                object result = checkCmd.ExecuteScalar();

                if (result == null)
                {
                    TempData["Error"] = "Feedback not found.";

                    return RedirectToAction("MyFeedback");
                }


                if (result != DBNull.Value &&
                    !string.IsNullOrWhiteSpace(result.ToString()))
                {
                    TempData["Error"] = "Feedback cannot be deleted because the admin has already replied.";

                    return RedirectToAction("MyFeedback");
                }


                string query = @"

UPDATE tbl_Feedback

SET

IsDeleted = 1,
UpdatedDate = @UpdatedDate

WHERE

FeedbackId = @FeedbackId
AND CustomerId = @CustomerId";

                SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@FeedbackId", id);

                cmd.Parameters.AddWithValue("@CustomerId", customerId);

                cmd.Parameters.AddWithValue("@UpdatedDate", DateTime.Now);

                int delete = cmd.ExecuteNonQuery();

                if (delete > 0)
                {
                    TempData["Success"] = "Feedback deleted successfully.";
                }
                else
                {
                    TempData["Error"] = "Unable to delete feedback.";
                }
            }

            return RedirectToAction("MyFeedback");
        }
       
        [HttpGet]
        public IActionResult Contact()
        {
            ContactInquiryModel model = new ContactInquiryModel();

            if (HttpContext.Session.GetString("CustomerId") != null)
            {
                long customerId = Convert.ToInt64(HttpContext.Session.GetString("CustomerId"));

                using (SqlConnection con = new SqlConnection(cs))
                {
                    con.Open();

                    string query = @"

SELECT

CustomerId,
FullName,
Email,
MobileNo

FROM tbl_Customer

WHERE

CustomerId=@CustomerId
AND IsDeleted=0
AND IsActive=1";

                    SqlCommand cmd = new SqlCommand(query, con);

                    cmd.Parameters.AddWithValue("@CustomerId", customerId);

                    SqlDataReader dr = cmd.ExecuteReader();

                    if (dr.Read())
                    {
                        model.CustomerId = Convert.ToInt64(dr["CustomerId"]);

                        model.FullName = dr["FullName"].ToString();

                        model.Email = dr["Email"].ToString();

                        model.MobileNo = dr["MobileNo"].ToString();
                    }

                    dr.Close();
                }
            }

            return View(model);
        }
     
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Contact(ContactInquiryModel model)
        {
            ModelState.Remove("CustomerName");
            ModelState.Remove("CustomerImage");
            ModelState.Remove("ReplyMessage");
            ModelState.Remove("StatusText");

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            long? customerId = null;

            if (HttpContext.Session.GetString("CustomerId") != null)
            {
                customerId = Convert.ToInt64(HttpContext.Session.GetString("CustomerId"));
            }

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

                string query = @"

INSERT INTO tbl_ContactInquiry
(
    CustomerId,
    FullName,
    Email,
    MobileNo,
    Subject,
    Message,
    ReplyMessage,
    IsReplied,
    IsActive,
    IsDeleted,
    CreatedDate
)

VALUES
(
    @CustomerId,
    @FullName,
    @Email,
    @MobileNo,
    @Subject,
    @Message,
    @ReplyMessage,
    @IsReplied,
    @IsActive,
    @IsDeleted,
    @CreatedDate
)";

                SqlCommand cmd = new SqlCommand(query, con);

                if (customerId.HasValue)
                {
                    cmd.Parameters.AddWithValue("@CustomerId", customerId.Value);
                }
                else
                {
                    cmd.Parameters.AddWithValue("@CustomerId", DBNull.Value);
                }

                cmd.Parameters.AddWithValue("@FullName", model.FullName);

                cmd.Parameters.AddWithValue("@Email", model.Email);

                cmd.Parameters.AddWithValue("@MobileNo", model.MobileNo);

                cmd.Parameters.AddWithValue("@Subject", model.Subject);

                cmd.Parameters.AddWithValue("@Message", model.Message);

                cmd.Parameters.AddWithValue("@ReplyMessage", DBNull.Value);

                cmd.Parameters.AddWithValue("@IsReplied", false);

                cmd.Parameters.AddWithValue("@IsActive", true);

                cmd.Parameters.AddWithValue("@IsDeleted", false);

                cmd.Parameters.AddWithValue("@CreatedDate", DateTime.Now);

                int result = cmd.ExecuteNonQuery();

                if (result > 0)
                {
                    TempData["Success"] = "Thank you! Your inquiry has been submitted successfully.";

                    return RedirectToAction("Contact");
                }
                else
                {
                    TempData["Error"] = "Unable to submit your inquiry. Please try again.";
                }
            }

            return View(model);
        }
      
        [HttpGet]
        public IActionResult MyContactInquiry(string search = "")
        {
            if (HttpContext.Session.GetString("CustomerId") == null)
            {
                return RedirectToAction("Login");
            }

            List<ContactInquiryModel> list = new List<ContactInquiryModel>();

            long customerId = Convert.ToInt64(HttpContext.Session.GetString("CustomerId"));

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

                string query = @"

SELECT

InquiryId,
CustomerId,
FullName,
Email,
MobileNo,
Subject,
Message,
ReplyMessage,
IsReplied,
IsActive,
IsDeleted,
CreatedDate,
UpdatedDate

FROM tbl_ContactInquiry

WHERE

CustomerId=@CustomerId
AND IsDeleted=0

AND
(
    @Search=''

    OR Subject LIKE @SearchText

    OR Message LIKE @SearchText
)

ORDER BY InquiryId DESC";

                SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@CustomerId", customerId);

                cmd.Parameters.AddWithValue("@Search", search ?? "");

                cmd.Parameters.AddWithValue("@SearchText", "%" + (search ?? "") + "%");

                SqlDataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    ContactInquiryModel model = new ContactInquiryModel();

                    model.InquiryId = Convert.ToInt64(dr["InquiryId"]);

                    model.CustomerId = dr["CustomerId"] == DBNull.Value
                        ? null
                        : Convert.ToInt64(dr["CustomerId"]);

                    model.FullName = dr["FullName"].ToString();

                    model.Email = dr["Email"].ToString();

                    model.MobileNo = dr["MobileNo"].ToString();

                    model.Subject = dr["Subject"].ToString();

                    model.Message = dr["Message"].ToString();

                    model.ReplyMessage = dr["ReplyMessage"] == DBNull.Value
                        ? ""
                        : dr["ReplyMessage"].ToString();

                    model.IsReplied = Convert.ToBoolean(dr["IsReplied"]);

                    model.IsActive = Convert.ToBoolean(dr["IsActive"]);

                    model.IsDeleted = Convert.ToBoolean(dr["IsDeleted"]);

                    model.CreatedDate = Convert.ToDateTime(dr["CreatedDate"]);

                    if (dr["UpdatedDate"] != DBNull.Value)
                    {
                        model.UpdatedDate = Convert.ToDateTime(dr["UpdatedDate"]);
                    }

                    list.Add(model);
                }

                dr.Close();
            }

            ViewBag.Search = search;

            return View(list);
        }
     
        [HttpGet]
        public IActionResult ViewContactInquiry(long id)
        {
            if (HttpContext.Session.GetString("CustomerId") == null)
            {
                return RedirectToAction("Login");
            }

            ContactInquiryModel model = new ContactInquiryModel();

            long customerId = Convert.ToInt64(HttpContext.Session.GetString("CustomerId"));

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

                string query = @"

SELECT

InquiryId,
CustomerId,
FullName,
Email,
MobileNo,
Subject,
Message,
ReplyMessage,
IsReplied,
IsActive,
IsDeleted,
CreatedDate,
UpdatedDate

FROM tbl_ContactInquiry

WHERE

InquiryId = @InquiryId
AND CustomerId = @CustomerId
AND IsDeleted = 0";

                SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@InquiryId", id);

                cmd.Parameters.AddWithValue("@CustomerId", customerId);

                SqlDataReader dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    model.InquiryId = Convert.ToInt64(dr["InquiryId"]);

                    model.CustomerId = dr["CustomerId"] == DBNull.Value
                        ? null
                        : Convert.ToInt64(dr["CustomerId"]);

                    model.FullName = dr["FullName"].ToString();

                    model.Email = dr["Email"].ToString();

                    model.MobileNo = dr["MobileNo"].ToString();

                    model.Subject = dr["Subject"].ToString();

                    model.Message = dr["Message"].ToString();

                    model.ReplyMessage = dr["ReplyMessage"] == DBNull.Value
                        ? ""
                        : dr["ReplyMessage"].ToString();

                    model.IsReplied = Convert.ToBoolean(dr["IsReplied"]);

                    model.IsActive = Convert.ToBoolean(dr["IsActive"]);

                    model.IsDeleted = Convert.ToBoolean(dr["IsDeleted"]);

                    model.CreatedDate = Convert.ToDateTime(dr["CreatedDate"]);

                    if (dr["UpdatedDate"] != DBNull.Value)
                    {
                        model.UpdatedDate = Convert.ToDateTime(dr["UpdatedDate"]);
                    }
                }
                else
                {
                    dr.Close();

                    TempData["Error"] = "Contact inquiry not found.";

                    return RedirectToAction("MyContactInquiry");
                }

                dr.Close();
            }

            return View(model);
        }
  
        [HttpGet]
        public IActionResult HealthTips(string search = "")
        {
            if (HttpContext.Session.GetString("CustomerId") == null)
            {
                return RedirectToAction("Login");
            }

            List<HealthTipModel> list = new List<HealthTipModel>();

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

                string query = @"

SELECT

HealthTipId,
Category,
Title,
ShortDescription,
Description,
TipImage,
AuthorName,
DisplayOrder,
IsFeatured,
CreatedDate

FROM tbl_HealthTip

WHERE

IsActive = 1
AND IsDeleted = 0

AND
(
    @Search=''

    OR Title LIKE @SearchText

    OR Category LIKE @SearchText

    OR AuthorName LIKE @SearchText
)

ORDER BY

IsFeatured DESC,
DisplayOrder ASC,
CreatedDate DESC";

                SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@Search", search ?? "");

                cmd.Parameters.AddWithValue("@SearchText", "%" + (search ?? "") + "%");

                SqlDataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    HealthTipModel model = new HealthTipModel();

                    model.HealthTipId = Convert.ToInt64(dr["HealthTipId"]);

                    model.Category = dr["Category"].ToString();

                    model.Title = dr["Title"].ToString();

                    model.ShortDescription = dr["ShortDescription"].ToString();

                    model.Description = dr["Description"].ToString();

                    model.TipImage = dr["TipImage"] == DBNull.Value
                                        ? ""
                                        : dr["TipImage"].ToString();

                    model.AuthorName = dr["AuthorName"].ToString();

                    model.DisplayOrder = Convert.ToInt32(dr["DisplayOrder"]);

                    model.IsFeatured = Convert.ToBoolean(dr["IsFeatured"]);

                    model.CreatedDate = Convert.ToDateTime(dr["CreatedDate"]);

                    list.Add(model);
                }

                dr.Close();
            }

            ViewBag.Search = search;

            return View(list);
        }
     
        [HttpGet]
        public IActionResult HealthTipDetails(long id)
        {
            if (HttpContext.Session.GetString("CustomerId") == null)
            {
                return RedirectToAction("Login");
            }

            HealthTipModel model = new HealthTipModel();

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

                string query = @"

SELECT

HealthTipId,
Category,
Title,
ShortDescription,
Description,
TipImage,
AuthorName,
DisplayOrder,
IsFeatured,
IsActive,
CreatedDate,
UpdatedDate

FROM tbl_HealthTip

WHERE

HealthTipId=@HealthTipId
AND IsActive=1
AND IsDeleted=0";

                SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@HealthTipId", id);

                SqlDataReader dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    model.HealthTipId = Convert.ToInt64(dr["HealthTipId"]);

                    model.Category = dr["Category"].ToString();

                    model.Title = dr["Title"].ToString();

                    model.ShortDescription = dr["ShortDescription"].ToString();

                    model.Description = dr["Description"].ToString();

                    model.TipImage = dr["TipImage"] == DBNull.Value
                                        ? ""
                                        : dr["TipImage"].ToString();

                    model.AuthorName = dr["AuthorName"].ToString();

                    model.DisplayOrder = Convert.ToInt32(dr["DisplayOrder"]);

                    model.IsFeatured = Convert.ToBoolean(dr["IsFeatured"]);

                    model.IsActive = Convert.ToBoolean(dr["IsActive"]);

                    model.CreatedDate = Convert.ToDateTime(dr["CreatedDate"]);

                    if (dr["UpdatedDate"] != DBNull.Value)
                    {
                        model.UpdatedDate = Convert.ToDateTime(dr["UpdatedDate"]);
                    }
                }
                else
                {
                    dr.Close();

                    TempData["Error"] = "Health Tip not found.";

                    return RedirectToAction("HealthTips");
                }

                dr.Close();
            }

            return View(model);
        }
      
        [HttpGet]
        public IActionResult HospitalHolidays(string search = "")
        {

            if (HttpContext.Session.GetString("CustomerId") == null)
            {
                return RedirectToAction("Login");
            }

            List<HospitalHolidayModel> list = new List<HospitalHolidayModel>();

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

                string query = @"

SELECT

HolidayId,
HolidayTitle,
HolidayDate,
HolidayDescription

FROM tbl_HospitalHoliday

WHERE

IsActive = 1
AND IsDeleted = 0
AND HolidayDate >= CAST(GETDATE() AS DATE)

";

                if (!string.IsNullOrWhiteSpace(search))
                {
                    query += @"

AND HolidayTitle LIKE @Search

";
                }

                query += @"

ORDER BY HolidayDate ASC

";

                SqlCommand cmd = new SqlCommand(query, con);

                if (!string.IsNullOrWhiteSpace(search))
                {
                    cmd.Parameters.AddWithValue("@Search", "%" + search + "%");
                }

                SqlDataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    HospitalHolidayModel model = new HospitalHolidayModel();

                    model.HolidayId = Convert.ToInt64(dr["HolidayId"]);

                    model.HolidayTitle = dr["HolidayTitle"].ToString();

                    model.HolidayDate = Convert.ToDateTime(dr["HolidayDate"]);

                    model.HolidayDescription = dr["HolidayDescription"] == DBNull.Value
                        ? ""
                        : dr["HolidayDescription"].ToString();

                    list.Add(model);
                }

                dr.Close();
            }

            ViewBag.Search = search;

            return View(list);
        }
      
        [HttpGet]
        public IActionResult AboutUs()
        {
            if (HttpContext.Session.GetString("CustomerId") == null)
            {
                return RedirectToAction("Login");
            }

            ClinicInformationModel model = new ClinicInformationModel();

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

                string query = @"

SELECT TOP 1

ClinicId,
ClinicName,
AboutClinic,
Mission,
Vision,
WhyChooseUs,
Address,
MobileNo,
AlternateMobileNo,
Email,
Website,
WorkingHours,
EmergencyContact,
GoogleMapLink,
FacebookLink,
InstagramLink,
TwitterLink,
WhatsAppNo,
ClinicLogo,
BannerImage

FROM tbl_ClinicInformation

WHERE IsActive = 1

ORDER BY ClinicId DESC";

                SqlCommand cmd = new SqlCommand(query, con);

                SqlDataReader dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    model.ClinicId = Convert.ToInt64(dr["ClinicId"]);

                    model.ClinicName = dr["ClinicName"].ToString();

                    model.AboutClinic = dr["AboutClinic"].ToString();

                    model.Mission = dr["Mission"] == DBNull.Value
                        ? ""
                        : dr["Mission"].ToString();

                    model.Vision = dr["Vision"] == DBNull.Value
                        ? ""
                        : dr["Vision"].ToString();

                    model.WhyChooseUs = dr["WhyChooseUs"] == DBNull.Value
                        ? ""
                        : dr["WhyChooseUs"].ToString();

                    model.Address = dr["Address"].ToString();

                    model.MobileNo = dr["MobileNo"].ToString();

                    model.AlternateMobileNo = dr["AlternateMobileNo"] == DBNull.Value
                        ? ""
                        : dr["AlternateMobileNo"].ToString();

                    model.Email = dr["Email"].ToString();

                    model.Website = dr["Website"] == DBNull.Value
                        ? ""
                        : dr["Website"].ToString();

                    model.WorkingHours = dr["WorkingHours"].ToString();

                    model.EmergencyContact = dr["EmergencyContact"] == DBNull.Value
                        ? ""
                        : dr["EmergencyContact"].ToString();

                    model.GoogleMapLink = dr["GoogleMapLink"] == DBNull.Value
                        ? ""
                        : dr["GoogleMapLink"].ToString();

                    model.FacebookLink = dr["FacebookLink"] == DBNull.Value
                        ? ""
                        : dr["FacebookLink"].ToString();

                    model.InstagramLink = dr["InstagramLink"] == DBNull.Value
                        ? ""
                        : dr["InstagramLink"].ToString();

                    model.TwitterLink = dr["TwitterLink"] == DBNull.Value
                        ? ""
                        : dr["TwitterLink"].ToString();

                    model.WhatsAppNo = dr["WhatsAppNo"] == DBNull.Value
                        ? ""
                        : dr["WhatsAppNo"].ToString();

                    model.ClinicLogo = dr["ClinicLogo"] == DBNull.Value
                        ? ""
                        : dr["ClinicLogo"].ToString();

                    model.BannerImage = dr["BannerImage"] == DBNull.Value
                        ? ""
                        : dr["BannerImage"].ToString();
                }

                dr.Close();
            }

            return View(model);
        }
        [HttpGet]
        public IActionResult EditProfile()
        {
            string? customerIdStr = HttpContext.Session.GetString("CustomerId");
            if (string.IsNullOrEmpty(customerIdStr))
            {
                return RedirectToAction("Login");
            }

            long customerId = Convert.ToInt64(customerIdStr);
            CustomerModel customer = new CustomerModel();

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();
                string query = "SELECT * FROM tbl_Customer WHERE CustomerId = @CustomerId AND IsDeleted = 0";
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@CustomerId", customerId);

                SqlDataReader dr = cmd.ExecuteReader();
                if (dr.Read())
                {
                    customer.CustomerId = Convert.ToInt64(dr["CustomerId"]);
                    customer.FullName = dr["FullName"] != DBNull.Value ? dr["FullName"].ToString() : "";
                    customer.Gender = dr["Gender"] != DBNull.Value ? dr["Gender"].ToString() : "";
                    customer.DateOfBirth = dr["DateOfBirth"] != DBNull.Value ? Convert.ToDateTime(dr["DateOfBirth"]) : null;
                    customer.BloodGroup = dr["BloodGroup"] != DBNull.Value ? dr["BloodGroup"].ToString() : "";
                    customer.MobileNo = dr["MobileNo"] != DBNull.Value ? dr["MobileNo"].ToString() : "";
                    customer.Email = dr["Email"] != DBNull.Value ? dr["Email"].ToString() : "";
                    customer.Address = dr["Address"] != DBNull.Value ? dr["Address"].ToString() : "";
                    customer.City = dr["City"] != DBNull.Value ? dr["City"].ToString() : "";
                    customer.State = dr["State"] != DBNull.Value ? dr["State"].ToString() : "";
                    customer.Pincode = dr["Pincode"] != DBNull.Value ? dr["Pincode"].ToString() : "";
                    customer.ProfileImage = dr["ProfileImage"] != DBNull.Value ? dr["ProfileImage"].ToString() : "";
                }
                dr.Close();
            }

            return View(customer);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditProfile(CustomerModel model)
        {
            string? customerIdStr = HttpContext.Session.GetString("CustomerId");
            if (string.IsNullOrEmpty(customerIdStr))
            {
                return RedirectToAction("Login");
            }

            ModelState.Remove("Password");
            ModelState.Remove("ConfirmPassword");
            ModelState.Remove("ProfileImage");

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                using (SqlConnection con = new SqlConnection(cs))
                {
                    con.Open();

                   
                    string oldImage = "";
                    SqlCommand oldImgCmd = new SqlCommand("SELECT ProfileImage FROM tbl_Customer WHERE CustomerId = @CustomerId", con);
                    oldImgCmd.Parameters.AddWithValue("@CustomerId", model.CustomerId);
                    object result = oldImgCmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        oldImage = result.ToString()!;
                    }

                    string fileName = oldImage;


                    if (model.ImageFile != null && model.ImageFile.Length > 0)
                    {
                        string folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "CustomerProfile");
                        if (!Directory.Exists(folderPath))
                        {
                            Directory.CreateDirectory(folderPath);
                        }

                        if (!string.IsNullOrEmpty(oldImage))
                        {
                            string oldFilePath = Path.Combine(folderPath, oldImage);
                            if (System.IO.File.Exists(oldFilePath))
                            {
                                System.IO.File.Delete(oldFilePath);
                            }
                        }

                        fileName = Guid.NewGuid().ToString() + Path.GetExtension(model.ImageFile.FileName);
                        string filePath = Path.Combine(folderPath, fileName);

                        using (FileStream stream = new FileStream(filePath, FileMode.Create))
                        {
                            model.ImageFile.CopyTo(stream);
                        }
                    }

                    string updateQuery = @"
                 UPDATE tbl_Customer
                 SET FullName = @FullName,
                     Gender = @Gender,
                     DateOfBirth = @DateOfBirth,
                     BloodGroup = @BloodGroup,
                     MobileNo = @MobileNo,
                     Address = @Address,
                     City = @City,
                     State = @State,
                     Pincode = @Pincode,
                     ProfileImage = @ProfileImage,
                     UpdatedDate = GETDATE()
                 WHERE CustomerId = @CustomerId AND IsDeleted = 0";

                    SqlCommand cmd = new SqlCommand(updateQuery, con);
                    cmd.Parameters.AddWithValue("@FullName", (object?)model.FullName ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Gender", (object?)model.Gender ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@DateOfBirth", (object?)model.DateOfBirth ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@BloodGroup", (object?)model.BloodGroup ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@MobileNo", (object?)model.MobileNo ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Address", (object?)model.Address ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@City", (object?)model.City ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@State", (object?)model.State ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Pincode", (object?)model.Pincode ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ProfileImage", string.IsNullOrEmpty(fileName) ? DBNull.Value : fileName);
                    cmd.Parameters.AddWithValue("@CustomerId", model.CustomerId);

                    cmd.ExecuteNonQuery();


                    if (!string.IsNullOrEmpty(model.FullName))
                    {
                        HttpContext.Session.SetString("CustomerName", model.FullName);
                    }
                    HttpContext.Session.SetString("CustomerImage", fileName ?? "");

                    TempData["Success"] = "Profile updated successfully!";
                    return RedirectToAction("Dashboard");
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = "An error occurred while updating profile: " + ex.Message;
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult MedicalCamps(
            string search = "",
            long departmentId = 0)
        {

            if (HttpContext.Session.GetString("CustomerId") == null)
            {
                return RedirectToAction("Login");
            }

            List<MedicalCampModel> list =
                new List<MedicalCampModel>();

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();


                string query = @"
            SELECT
                mc.CampId,
                mc.CampTitle,
                mc.CampImage,
                mc.CampBanner,
                mc.CampDescription,
                mc.CampDate,
                mc.StartTime,
                mc.EndTime,
                mc.Venue,
                mc.Organizer,

                mc.DoctorId,
                d.DoctorName,

                mc.DepartmentId,
                dep.DepartmentName,

                mc.RegistrationFee,
                mc.MaxParticipants,
                mc.AvailableSeats,

                mc.ContactNumber,
                mc.Email,
                mc.Benefits,
                mc.Instructions,

                mc.IsFeatured,
                mc.IsActive,
                mc.IsDeleted,

                mc.CreatedDate,
                mc.UpdatedDate

            FROM tbl_MedicalCamp mc

            INNER JOIN tbl_Doctor d
                ON mc.DoctorId = d.DoctorId

            INNER JOIN tbl_Department dep
                ON mc.DepartmentId = dep.DepartmentId

            WHERE
                mc.IsActive = 1
                AND mc.IsDeleted = 0

                AND mc.CampDate >= CAST(GETDATE() AS DATE)

                AND d.IsActive = 1
                AND d.IsDeleted = 0

                AND dep.IsActive = 1
                AND dep.IsDeleted = 0
        ";


                if (!string.IsNullOrWhiteSpace(search))
                {
                    query += @"
                AND
                (
                    mc.CampTitle LIKE @Search
                    OR d.DoctorName LIKE @Search
                    OR dep.DepartmentName LIKE @Search
                    OR mc.Venue LIKE @Search
                )";
                }

                if (departmentId > 0)
                {
                    query += @"
                AND mc.DepartmentId = @DepartmentId";
                }


                query += @"
            ORDER BY
                mc.IsFeatured DESC,
                mc.CampDate ASC,
                mc.StartTime ASC";

                SqlCommand cmd =
                    new SqlCommand(query, con);


                if (!string.IsNullOrWhiteSpace(search))
                {
                    cmd.Parameters.AddWithValue(
                        "@Search",
                        "%" + search.Trim() + "%");
                }


                if (departmentId > 0)
                {
                    cmd.Parameters.AddWithValue(
                        "@DepartmentId",
                        departmentId);
                }


                SqlDataReader dr =
                    cmd.ExecuteReader();

                while (dr.Read())
                {
                    MedicalCampModel model =
                        new MedicalCampModel();


                    model.CampId =
                        Convert.ToInt64(dr["CampId"]);

                    model.CampTitle =
                        dr["CampTitle"].ToString();

                    model.CampDescription =
                        dr["CampDescription"].ToString();


                    model.CampImage =
                        dr["CampImage"] == DBNull.Value
                        ? ""
                        : dr["CampImage"].ToString();

                    model.CampBanner =
                        dr["CampBanner"] == DBNull.Value
                        ? ""
                        : dr["CampBanner"].ToString();


                    if (dr["CampDate"] != DBNull.Value)
                    {
                        model.CampDate =
                            Convert.ToDateTime(dr["CampDate"]);
                    }

                    if (dr["StartTime"] != DBNull.Value)
                    {
                        model.StartTime =
                            (TimeSpan)dr["StartTime"];
                    }

                    if (dr["EndTime"] != DBNull.Value)
                    {
                        model.EndTime =
                            (TimeSpan)dr["EndTime"];
                    }


                    model.Venue =
                        dr["Venue"].ToString();

                    model.Organizer =
                        dr["Organizer"] == DBNull.Value
                        ? ""
                        : dr["Organizer"].ToString();


                    model.DoctorId =
                        Convert.ToInt64(dr["DoctorId"]);

                    model.DoctorName =
                        dr["DoctorName"].ToString();


                    model.DepartmentId =
                        Convert.ToInt64(dr["DepartmentId"]);

                    model.DepartmentName =
                        dr["DepartmentName"].ToString();


                    model.RegistrationFee =
                        Convert.ToDecimal(
                            dr["RegistrationFee"]);

                    model.MaxParticipants =
                        Convert.ToInt32(
                            dr["MaxParticipants"]);

                    model.AvailableSeats =
                        Convert.ToInt32(
                            dr["AvailableSeats"]);


                    model.ContactNumber =
                        dr["ContactNumber"].ToString();

                    model.Email =
                        dr["Email"] == DBNull.Value
                        ? ""
                        : dr["Email"].ToString();


                    model.Benefits =
                        dr["Benefits"] == DBNull.Value
                        ? ""
                        : dr["Benefits"].ToString();

                    model.Instructions =
                        dr["Instructions"] == DBNull.Value
                        ? ""
                        : dr["Instructions"].ToString();


                    model.IsFeatured =
                        Convert.ToBoolean(
                            dr["IsFeatured"]);

                    model.IsActive =
                        Convert.ToBoolean(
                            dr["IsActive"]);

                    model.IsDeleted =
                        Convert.ToBoolean(
                            dr["IsDeleted"]);


                    model.CreatedDate =
                        Convert.ToDateTime(
                            dr["CreatedDate"]);

                    if (dr["UpdatedDate"] != DBNull.Value)
                    {
                        model.UpdatedDate =
                            Convert.ToDateTime(
                                dr["UpdatedDate"]);
                    }

                    list.Add(model);
                }

                dr.Close();


                List<DepartmentModel> departments =
                    new List<DepartmentModel>();

                SqlCommand deptCmd =
                    new SqlCommand(@"
                SELECT
                    DepartmentId,
                    DepartmentName
                FROM tbl_Department
                WHERE
                    IsActive = 1
                    AND IsDeleted = 0
                ORDER BY DepartmentName ASC", con);

                SqlDataReader deptDr =
                    deptCmd.ExecuteReader();

                while (deptDr.Read())
                {
                    departments.Add(
                        new DepartmentModel
                        {
                            DepartmentId =
                                Convert.ToInt64(
                                    deptDr["DepartmentId"]),

                            DepartmentName =
                                deptDr["DepartmentName"]
                                .ToString()
                        });
                }

                deptDr.Close();


                ViewBag.DepartmentList =
                    departments;

                ViewBag.Search =
                    search;

                ViewBag.DepartmentId =
                    departmentId;
            }

            return View(list);
        }
     
        [HttpGet]
        public IActionResult MedicalCampDetails(long id)
        {

            if (HttpContext.Session.GetString("CustomerId") == null)
            {
                return RedirectToAction("Login");
            }

            MedicalCampModel model = new MedicalCampModel();

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();


                string query = @"
            SELECT
                mc.CampId,
                mc.CampTitle,
                mc.CampImage,
                mc.CampBanner,
                mc.CampDescription,
                mc.CampDate,
                mc.StartTime,
                mc.EndTime,
                mc.Venue,
                mc.Organizer,

                mc.DoctorId,
                d.DoctorName,

                mc.DepartmentId,
                dep.DepartmentName,

                mc.RegistrationFee,
                mc.MaxParticipants,
                mc.AvailableSeats,

                mc.ContactNumber,
                mc.Email,
                mc.Benefits,
                mc.Instructions,

                mc.IsFeatured,
                mc.IsActive,
                mc.IsDeleted,

                mc.CreatedDate,
                mc.UpdatedDate

            FROM tbl_MedicalCamp mc

            INNER JOIN tbl_Doctor d
                ON mc.DoctorId = d.DoctorId

            INNER JOIN tbl_Department dep
                ON mc.DepartmentId = dep.DepartmentId

            WHERE
                mc.CampId = @CampId
                AND mc.IsActive = 1
                AND mc.IsDeleted = 0

                AND d.IsActive = 1
                AND d.IsDeleted = 0

                AND dep.IsActive = 1
                AND dep.IsDeleted = 0";

                SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@CampId", id);

                SqlDataReader dr = cmd.ExecuteReader();


                if (dr.Read())
                {

                    model.CampId =
                        Convert.ToInt64(dr["CampId"]);

                    model.CampTitle =
                        dr["CampTitle"].ToString();

                    model.CampDescription =
                        dr["CampDescription"] == DBNull.Value
                            ? ""
                            : dr["CampDescription"].ToString();


                  

                    model.CampImage =
                        dr["CampImage"] == DBNull.Value
                            ? ""
                            : dr["CampImage"].ToString();

                    model.CampBanner =
                        dr["CampBanner"] == DBNull.Value
                            ? ""
                            : dr["CampBanner"].ToString();


                  

                    if (dr["CampDate"] != DBNull.Value)
                    {
                        model.CampDate =
                            Convert.ToDateTime(dr["CampDate"]);
                    }


                  

                    if (dr["StartTime"] != DBNull.Value)
                    {
                        model.StartTime =
                            (TimeSpan)dr["StartTime"];
                    }

                    if (dr["EndTime"] != DBNull.Value)
                    {
                        model.EndTime =
                            (TimeSpan)dr["EndTime"];
                    }



                    model.Venue =
                        dr["Venue"] == DBNull.Value
                            ? ""
                            : dr["Venue"].ToString();



                    model.Organizer =
                        dr["Organizer"] == DBNull.Value
                            ? ""
                            : dr["Organizer"].ToString();


                    

                    model.DoctorId =
                        Convert.ToInt64(dr["DoctorId"]);

                    model.DoctorName =
                        dr["DoctorName"].ToString();


                    

                    model.DepartmentId =
                        Convert.ToInt64(dr["DepartmentId"]);

                    model.DepartmentName =
                        dr["DepartmentName"].ToString();


                    

                    model.RegistrationFee =
                        Convert.ToDecimal(
                            dr["RegistrationFee"]);

                    model.MaxParticipants =
                        Convert.ToInt32(
                            dr["MaxParticipants"]);

                    model.AvailableSeats =
                        Convert.ToInt32(
                            dr["AvailableSeats"]);


                    

                    model.ContactNumber =
                        dr["ContactNumber"] == DBNull.Value
                            ? ""
                            : dr["ContactNumber"].ToString();

                    model.Email =
                        dr["Email"] == DBNull.Value
                            ? ""
                            : dr["Email"].ToString();


                    

                    model.Benefits =
                        dr["Benefits"] == DBNull.Value
                            ? ""
                            : dr["Benefits"].ToString();



                    model.Instructions =
                        dr["Instructions"] == DBNull.Value
                            ? ""
                            : dr["Instructions"].ToString();



                    model.IsFeatured =
                        Convert.ToBoolean(
                            dr["IsFeatured"]);

                    model.IsActive =
                        Convert.ToBoolean(
                            dr["IsActive"]);

                    model.IsDeleted =
                        Convert.ToBoolean(
                            dr["IsDeleted"]);



                    if (dr["CreatedDate"] != DBNull.Value)
                    {
                        model.CreatedDate =
                            Convert.ToDateTime(
                                dr["CreatedDate"]);
                    }

                    if (dr["UpdatedDate"] != DBNull.Value)
                    {
                        model.UpdatedDate =
                            Convert.ToDateTime(
                                dr["UpdatedDate"]);
                    }
                }
                else
                {
                    dr.Close();

                    TempData["Error"] =
                        "Medical Camp not found or is no longer available.";

                    return RedirectToAction("MedicalCamps");
                }

                dr.Close();
            }


            return View(model);
        }
        //======================================================
        // REGISTER CAMP - GET
        //======================================================
        [HttpGet]
        public IActionResult RegisterCamp(long id)
        {
            // Login check
            string customerIdSession =
                HttpContext.Session.GetString("CustomerId");

            if (string.IsNullOrEmpty(customerIdSession))
            {
                return RedirectToAction("Login");
            }

            long customerId =
                Convert.ToInt64(customerIdSession);


            if (id <= 0)
            {
                TempData["Error"] = "Invalid Medical Camp.";

                return RedirectToAction("MedicalCamps");
            }


            CampRegistrationModel model =
                new CampRegistrationModel();


            try
            {
                using (SqlConnection con =
                    new SqlConnection(cs))
                {
                    con.Open();


                    //==========================================
                    // CAMP DETAILS
                    //==========================================

                    string campQuery = @"
SELECT
    mc.CampId,
    mc.CampTitle,
    mc.CampImage,
    mc.CampBanner,
    mc.CampDescription,
    mc.CampDate,
    mc.StartTime,
    mc.EndTime,
    mc.Venue,
    mc.Organizer,

    d.DoctorName,

    dep.DepartmentName,

    mc.RegistrationFee,
    mc.MaxParticipants,
    mc.AvailableSeats,

    mc.ContactNumber,
    mc.Email,
    mc.Benefits,
    mc.Instructions

FROM tbl_MedicalCamp mc

INNER JOIN tbl_Doctor d
    ON mc.DoctorId = d.DoctorId

INNER JOIN tbl_Department dep
    ON mc.DepartmentId = dep.DepartmentId

WHERE
    mc.CampId = @CampId

    AND mc.IsActive = 1
    AND mc.IsDeleted = 0

    AND d.IsActive = 1
    AND d.IsDeleted = 0

    AND dep.IsActive = 1
    AND dep.IsDeleted = 0

    AND mc.CampDate >= CAST(GETDATE() AS DATE)";


                    using (SqlCommand cmd =
                        new SqlCommand(campQuery, con))
                    {
                        cmd.Parameters.Add(
                            "@CampId",
                            SqlDbType.BigInt).Value = id;


                        using (SqlDataReader dr =
                            cmd.ExecuteReader())
                        {
                            if (!dr.Read())
                            {
                                TempData["Error"] =
                                    "Medical Camp not found or registration is closed.";

                                return RedirectToAction(
                                    "MedicalCamps");
                            }


                            model.CampId =
                                Convert.ToInt64(
                                    dr["CampId"]);


                            model.CampTitle =
                                dr["CampTitle"] == DBNull.Value
                                ? ""
                                : dr["CampTitle"].ToString();


                            model.CampImage =
                                dr["CampImage"] == DBNull.Value
                                ? ""
                                : dr["CampImage"].ToString();


                            model.CampBanner =
                                dr["CampBanner"] == DBNull.Value
                                ? ""
                                : dr["CampBanner"].ToString();


                            model.CampDescription =
                                dr["CampDescription"] == DBNull.Value
                                ? ""
                                : dr["CampDescription"].ToString();


                            if (dr["CampDate"] != DBNull.Value)
                            {
                                model.CampDate =
                                    Convert.ToDateTime(
                                        dr["CampDate"]);
                            }


                            if (dr["StartTime"] != DBNull.Value)
                            {
                                model.StartTime =
                                    (TimeSpan)dr["StartTime"];
                            }


                            if (dr["EndTime"] != DBNull.Value)
                            {
                                model.EndTime =
                                    (TimeSpan)dr["EndTime"];
                            }


                            model.Venue =
                                dr["Venue"] == DBNull.Value
                                ? ""
                                : dr["Venue"].ToString();


                            model.Organizer =
                                dr["Organizer"] == DBNull.Value
                                ? ""
                                : dr["Organizer"].ToString();


                            model.DoctorName =
                                dr["DoctorName"] == DBNull.Value
                                ? ""
                                : dr["DoctorName"].ToString();


                            model.DepartmentName =
                                dr["DepartmentName"] == DBNull.Value
                                ? ""
                                : dr["DepartmentName"].ToString();


                            model.RegistrationFee =
                                dr["RegistrationFee"] == DBNull.Value
                                ? 0
                                : Convert.ToDecimal(
                                    dr["RegistrationFee"]);


                            model.MaxParticipants =
                                dr["MaxParticipants"] == DBNull.Value
                                ? 0
                                : Convert.ToInt32(
                                    dr["MaxParticipants"]);


                            model.AvailableSeats =
                                dr["AvailableSeats"] == DBNull.Value
                                ? 0
                                : Convert.ToInt32(
                                    dr["AvailableSeats"]);


                            model.ContactNumber =
                                dr["ContactNumber"] == DBNull.Value
                                ? ""
                                : dr["ContactNumber"].ToString();


                            // IMPORTANT
                            // DB = Email
                            // Model = CampEmail

                            model.CampEmail =
                                dr["Email"] == DBNull.Value
                                ? ""
                                : dr["Email"].ToString();


                            model.Benefits =
                                dr["Benefits"] == DBNull.Value
                                ? ""
                                : dr["Benefits"].ToString();


                            model.Instructions =
                                dr["Instructions"] == DBNull.Value
                                ? ""
                                : dr["Instructions"].ToString();
                        }
                    }


                    //==========================================
                    // SEAT CHECK
                    //==========================================

                    if (model.AvailableSeats <= 0)
                    {
                        TempData["Error"] =
                            "Sorry, this medical camp is full.";

                        return RedirectToAction(
                            "MedicalCampDetails",
                            new
                            {
                                id = id
                            });
                    }


                    //==========================================
                    // DUPLICATE CHECK
                    //==========================================

                    string duplicateQuery = @"
SELECT COUNT(*)
FROM tbl_CampRegistration

WHERE
    CampId = @CampId

    AND CustomerId = @CustomerId

    AND IsDeleted = 0

    AND RegistrationStatus IN
    (
        'Pending',
        'Confirmed'
    )";


                    using (SqlCommand duplicateCmd =
                        new SqlCommand(
                            duplicateQuery,
                            con))
                    {
                        duplicateCmd.Parameters.Add(
                            "@CampId",
                            SqlDbType.BigInt).Value =
                            id;


                        duplicateCmd.Parameters.Add(
                            "@CustomerId",
                            SqlDbType.BigInt).Value =
                            customerId;


                        int count =
                            Convert.ToInt32(
                                duplicateCmd.ExecuteScalar());


                        if (count > 0)
                        {
                            TempData["Error"] =
                                "You are already registered for this medical camp.";

                            return RedirectToAction(
                                "MedicalCampDetails",
                                new
                                {
                                    id = id
                                });
                        }
                    }


                    //==========================================
                    // CUSTOMER DETAILS
                    //==========================================

                    string customerQuery = @"
SELECT
    CustomerId,
    FullName,
    MobileNo,
    Email,
    Gender

FROM tbl_Customer

WHERE
    CustomerId = @CustomerId

    AND IsActive = 1
    AND IsDeleted = 0";


                    using (SqlCommand customerCmd =
                        new SqlCommand(
                            customerQuery,
                            con))
                    {
                        customerCmd.Parameters.Add(
                            "@CustomerId",
                            SqlDbType.BigInt).Value =
                            customerId;


                        using (SqlDataReader customerDr =
                            customerCmd.ExecuteReader())
                        {
                            if (!customerDr.Read())
                            {
                                TempData["Error"] =
                                    "Customer information could not be found.";

                                return RedirectToAction(
                                    "MedicalCamps");
                            }


                            model.CustomerId =
                                customerId;


                            // tbl_Customer has FullName
                            model.ParticipantName =
                                customerDr["FullName"] == DBNull.Value
                                ? ""
                                : customerDr["FullName"].ToString();


                            model.MobileNo =
                                customerDr["MobileNo"] == DBNull.Value
                                ? ""
                                : customerDr["MobileNo"].ToString();


                            model.Email =
                                customerDr["Email"] == DBNull.Value
                                ? ""
                                : customerDr["Email"].ToString();


                            model.Gender =
                                customerDr["Gender"] == DBNull.Value
                                ? ""
                                : customerDr["Gender"].ToString();
                        }
                    }
                }


                //==========================================
                // DEFAULT VALUES
                //==========================================

                model.RegistrationDate =
                    DateTime.Now;

                model.RegistrationStatus =
                    "Pending";

                model.PaymentStatus =
                    model.RegistrationFee > 0
                    ? "Pending"
                    : "NotRequired";

                model.Amount =
                    model.RegistrationFee;

                model.IsActive = true;

                model.IsDeleted = false;


                //==========================================
                // GENDER
                //==========================================

                ViewBag.GenderList =
                    new List<SelectListItem>
                    {
                new SelectListItem
                {
                    Text = "Select Gender",
                    Value = ""
                },

                new SelectListItem
                {
                    Text = "Male",
                    Value = "Male"
                },

                new SelectListItem
                {
                    Text = "Female",
                    Value = "Female"
                },

                new SelectListItem
                {
                    Text = "Other",
                    Value = "Other"
                }
                    };


                return View(model);
            }
            catch (Exception ex)
            {
                TempData["Error"] =
                    "Unable to load medical camp registration page: "
                    + ex.Message;

                return RedirectToAction(
                    "MedicalCamps");
            }
        }
        //======================================================
        // REGISTER CAMP - POST
        //======================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RegisterCamp(CampRegistrationModel model)
        {
            //==================================================
            // CUSTOMER LOGIN
            //==================================================

            string customerSession =
                HttpContext.Session.GetString("CustomerId");

            if (string.IsNullOrEmpty(customerSession))
            {
                return RedirectToAction("Login");
            }

            long customerId =
                Convert.ToInt64(customerSession);

            model.CustomerId = customerId;


            //==================================================
            // REMOVE DISPLAY ONLY VALIDATION
            //==================================================

            ModelState.Remove("CampTitle");
            ModelState.Remove("CampImage");
            ModelState.Remove("CampBanner");
            ModelState.Remove("CampDescription");

            ModelState.Remove("CampDate");
            ModelState.Remove("StartTime");
            ModelState.Remove("EndTime");

            ModelState.Remove("Venue");
            ModelState.Remove("Organizer");
            ModelState.Remove("Benefits");
            ModelState.Remove("Instructions");

            ModelState.Remove("ContactNumber");
            ModelState.Remove("CampEmail");

            ModelState.Remove("DoctorName");
            ModelState.Remove("DepartmentName");

            ModelState.Remove("RegistrationNo");
            ModelState.Remove("RegistrationDate");
            ModelState.Remove("RegistrationStatus");
            ModelState.Remove("PaymentStatus");
            ModelState.Remove("Amount");
            ModelState.Remove("AdminRemark");

            ModelState.Remove("IsActive");
            ModelState.Remove("IsDeleted");
            ModelState.Remove("CreatedDate");
            ModelState.Remove("UpdatedDate");


            //==================================================
            // BASIC VALIDATION
            //==================================================

            if (model.CampId <= 0)
            {
                TempData["Error"] =
                    "Invalid medical camp.";

                return RedirectToAction("MedicalCamps");
            }


            if (string.IsNullOrWhiteSpace(model.ParticipantName))
            {
                ModelState.AddModelError(
                    "ParticipantName",
                    "Participant Name is required.");
            }


            if (string.IsNullOrWhiteSpace(model.MobileNo))
            {
                ModelState.AddModelError(
                    "MobileNo",
                    "Mobile Number is required.");
            }


            if (string.IsNullOrWhiteSpace(model.Email))
            {
                ModelState.AddModelError(
                    "Email",
                    "Email is required.");
            }


            if (string.IsNullOrWhiteSpace(model.Gender))
            {
                ModelState.AddModelError(
                    "Gender",
                    "Gender is required.");
            }


            if (!ModelState.IsValid)
            {
                LoadCampRegistrationDetails(model);

                return View(model);
            }


            try
            {
                using (SqlConnection con =
                    new SqlConnection(cs))
                {
                    con.Open();


                    //==================================================
                    // TRANSACTION
                    //==================================================

                    using (SqlTransaction transaction =
                        con.BeginTransaction())
                    {
                        try
                        {
                            //==========================================
                            // GET CAMP DETAILS
                            //==========================================

                            string campQuery = @"
SELECT
    RegistrationFee,
    AvailableSeats,
    CampDate
FROM tbl_MedicalCamp
WHERE
    CampId = @CampId
    AND IsActive = 1
    AND IsDeleted = 0";


                            decimal registrationFee = 0;

                            int availableSeats = 0;

                            DateTime campDate;


                            using (SqlCommand campCmd =
                                new SqlCommand(
                                    campQuery,
                                    con,
                                    transaction))
                            {
                                campCmd.Parameters.Add(
                                    "@CampId",
                                    SqlDbType.BigInt).Value =
                                    model.CampId;


                                using (SqlDataReader dr =
                                    campCmd.ExecuteReader())
                                {
                                    if (!dr.Read())
                                    {
                                        transaction.Rollback();

                                        TempData["Error"] =
                                            "Medical camp not found.";

                                        return RedirectToAction(
                                            "MedicalCamps");
                                    }


                                    registrationFee =
                                        dr["RegistrationFee"] ==
                                        DBNull.Value
                                        ? 0
                                        : Convert.ToDecimal(
                                            dr["RegistrationFee"]);


                                    availableSeats =
                                        dr["AvailableSeats"] ==
                                        DBNull.Value
                                        ? 0
                                        : Convert.ToInt32(
                                            dr["AvailableSeats"]);


                                    campDate =
                                        Convert.ToDateTime(
                                            dr["CampDate"]);
                                }
                            }


                            //==========================================
                            // DATE CHECK
                            //==========================================

                            if (campDate.Date < DateTime.Today)
                            {
                                transaction.Rollback();

                                TempData["Error"] =
                                    "Registration for this medical camp is closed.";

                                return RedirectToAction(
                                    "MedicalCampDetails",
                                    new
                                    {
                                        id = model.CampId
                                    });
                            }


                            //==========================================
                            // SEAT CHECK
                            //==========================================

                            if (availableSeats <= 0)
                            {
                                transaction.Rollback();

                                TempData["Error"] =
                                    "Sorry, no seats are available.";

                                return RedirectToAction(
                                    "MedicalCampDetails",
                                    new
                                    {
                                        id = model.CampId
                                    });
                            }


                            //==========================================
                            // DUPLICATE REGISTRATION CHECK
                            //==========================================

                            string duplicateQuery = @"
SELECT COUNT(*)
FROM tbl_CampRegistration
WHERE
    CampId = @CampId
    AND CustomerId = @CustomerId
    AND IsDeleted = 0
    AND RegistrationStatus IN
    (
        'Pending',
        'Confirmed'
    )";


                            using (SqlCommand duplicateCmd =
                                new SqlCommand(
                                    duplicateQuery,
                                    con,
                                    transaction))
                            {
                                duplicateCmd.Parameters.Add(
                                    "@CampId",
                                    SqlDbType.BigInt).Value =
                                    model.CampId;


                                duplicateCmd.Parameters.Add(
                                    "@CustomerId",
                                    SqlDbType.BigInt).Value =
                                    customerId;


                                int count =
                                    Convert.ToInt32(
                                        duplicateCmd.ExecuteScalar());


                                if (count > 0)
                                {
                                    transaction.Rollback();

                                    TempData["Error"] =
                                        "You are already registered for this medical camp.";

                                    return RedirectToAction(
                                        "MedicalCampDetails",
                                        new
                                        {
                                            id = model.CampId
                                        });
                                }
                            }


                            //==========================================
                            // REGISTRATION NUMBER
                            //==========================================

                            string registrationNo =
                                "CAM-" +
                                DateTime.Now.ToString(
                                    "yyyyMMddHHmmssfff");


                            //==========================================
                            // PAYMENT STATUS
                            //==========================================

                            string paymentStatus =
                                registrationFee > 0
                                ? "Pending"
                                : "NotRequired";


                            //==========================================
                            // INSERT REGISTRATION
                            //==========================================

                            string insertQuery = @"
INSERT INTO tbl_CampRegistration
(
    CampId,
    CustomerId,
    RegistrationNo,
    RegistrationDate,
    ParticipantName,
    MobileNo,
    Email,
    Age,
    Gender,
    HealthConcern,
    RegistrationStatus,
    PaymentStatus,
    Amount,
    AdminRemark,
    IsActive,
    IsDeleted,
    CreatedDate,
    UpdatedDate
)
VALUES
(
    @CampId,
    @CustomerId,
    @RegistrationNo,
    @RegistrationDate,
    @ParticipantName,
    @MobileNo,
    @Email,
    @Age,
    @Gender,
    @HealthConcern,
    @RegistrationStatus,
    @PaymentStatus,
    @Amount,
    @AdminRemark,
    @IsActive,
    @IsDeleted,
    @CreatedDate,
    @UpdatedDate
)";


                            using (SqlCommand insertCmd =
                                new SqlCommand(
                                    insertQuery,
                                    con,
                                    transaction))
                            {
                                insertCmd.Parameters.Add(
                                    "@CampId",
                                    SqlDbType.BigInt).Value =
                                    model.CampId;


                                insertCmd.Parameters.Add(
                                    "@CustomerId",
                                    SqlDbType.BigInt).Value =
                                    customerId;


                                insertCmd.Parameters.Add(
                                    "@RegistrationNo",
                                    SqlDbType.NVarChar, 100).Value =
                                    registrationNo;


                                insertCmd.Parameters.Add(
                                    "@RegistrationDate",
                                    SqlDbType.DateTime).Value =
                                    DateTime.Now;


                                insertCmd.Parameters.Add(
                                    "@ParticipantName",
                                    SqlDbType.NVarChar, 200).Value =
                                    model.ParticipantName.Trim();


                                insertCmd.Parameters.Add(
                                    "@MobileNo",
                                    SqlDbType.NVarChar, 50).Value =
                                    model.MobileNo.Trim();


                                insertCmd.Parameters.Add(
                                    "@Email",
                                    SqlDbType.NVarChar, 200).Value =
                                    model.Email.Trim();


                                insertCmd.Parameters.Add(
                                    "@Age",
                                    SqlDbType.Int).Value =
                                    model.Age.HasValue
                                    ? (object)model.Age.Value
                                    : DBNull.Value;


                                insertCmd.Parameters.Add(
                                    "@Gender",
                                    SqlDbType.NVarChar, 50).Value =
                                    model.Gender.Trim();


                                insertCmd.Parameters.Add(
                                    "@HealthConcern",
                                    SqlDbType.NVarChar, -1).Value =
                                    string.IsNullOrWhiteSpace(
                                        model.HealthConcern)
                                    ? DBNull.Value
                                    : (object)
                                        model.HealthConcern.Trim();


                                insertCmd.Parameters.Add(
                                    "@RegistrationStatus",
                                    SqlDbType.NVarChar, 50).Value =
                                    "Pending";


                                insertCmd.Parameters.Add(
                                    "@PaymentStatus",
                                    SqlDbType.NVarChar, 50).Value =
                                    paymentStatus;


                                SqlParameter amountParameter =
                                    insertCmd.Parameters.Add(
                                        "@Amount",
                                        SqlDbType.Decimal);

                                amountParameter.Precision = 18;
                                amountParameter.Scale = 2;
                                amountParameter.Value =
                                    registrationFee;


                                insertCmd.Parameters.Add(
                                    "@AdminRemark",
                                    SqlDbType.NVarChar, -1).Value =
                                    DBNull.Value;


                                insertCmd.Parameters.Add(
                                    "@IsActive",
                                    SqlDbType.Bit).Value =
                                    true;


                                insertCmd.Parameters.Add(
                                    "@IsDeleted",
                                    SqlDbType.Bit).Value =
                                    false;


                                insertCmd.Parameters.Add(
                                    "@CreatedDate",
                                    SqlDbType.DateTime).Value =
                                    DateTime.Now;


                                insertCmd.Parameters.Add(
                                    "@UpdatedDate",
                                    SqlDbType.DateTime).Value =
                                    DBNull.Value;


                                int inserted =
                                    insertCmd.ExecuteNonQuery();


                                if (inserted != 1)
                                {
                                    transaction.Rollback();

                                    TempData["Error"] =
                                        "Registration could not be saved.";

                                    LoadCampRegistrationDetails(model);

                                    return View(model);
                                }
                            }


                            //==========================================
                            // DECREASE AVAILABLE SEATS
                            //==========================================

                            string seatQuery = @"
UPDATE tbl_MedicalCamp
SET
    AvailableSeats = AvailableSeats - 1,
    UpdatedDate = @UpdatedDate
WHERE
    CampId = @CampId
    AND AvailableSeats > 0
    AND IsActive = 1
    AND IsDeleted = 0";


                            using (SqlCommand seatCmd =
                                new SqlCommand(
                                    seatQuery,
                                    con,
                                    transaction))
                            {
                                seatCmd.Parameters.Add(
                                    "@CampId",
                                    SqlDbType.BigInt).Value =
                                    model.CampId;


                                seatCmd.Parameters.Add(
                                    "@UpdatedDate",
                                    SqlDbType.DateTime).Value =
                                    DateTime.Now;


                                int seatUpdated =
                                    seatCmd.ExecuteNonQuery();


                                if (seatUpdated != 1)
                                {
                                    transaction.Rollback();

                                    TempData["Error"] =
                                        "Registration saved but seat could not be reserved.";

                                    LoadCampRegistrationDetails(model);

                                    return View(model);
                                }
                            }


                            //==========================================
                            // COMMIT
                            //==========================================

                            transaction.Commit();


                            //==========================================
                            // SUCCESS
                            //==========================================

                            TempData["Success"] =
                                "Medical Camp registration successful! Registration No: "
                                + registrationNo;


                            //==========================================
                            // REDIRECT
                            //==========================================

                            return RedirectToAction(
                                "MyCampRegistrations");
                        }
                        catch (Exception ex)
                        {
                            try
                            {
                                transaction.Rollback();
                            }
                            catch
                            {
                            }


                            TempData["Error"] =
                                "Registration failed: " +
                                ex.Message;


                            LoadCampRegistrationDetails(model);

                            return View(model);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] =
                    "Database error: " +
                    ex.Message;


                LoadCampRegistrationDetails(model);

                return View(model);
            }
        }
        private void LoadCampRegistrationDetails(
    CampRegistrationModel model)
        {
            using (SqlConnection con =
                new SqlConnection(cs))
            {
                con.Open();


                string query = @"
SELECT
    mc.CampId,
    mc.CampTitle,
    mc.CampImage,
    mc.CampBanner,
    mc.CampDescription,
    mc.CampDate,
    mc.StartTime,
    mc.EndTime,
    mc.Venue,
    mc.Organizer,

    d.DoctorName,

    dep.DepartmentName,

    mc.RegistrationFee,
    mc.MaxParticipants,
    mc.AvailableSeats,

    mc.ContactNumber,
    mc.Email,
    mc.Benefits,
    mc.Instructions

FROM tbl_MedicalCamp mc

INNER JOIN tbl_Doctor d
    ON mc.DoctorId = d.DoctorId

INNER JOIN tbl_Department dep
    ON mc.DepartmentId = dep.DepartmentId

WHERE
    mc.CampId = @CampId

    AND mc.IsActive = 1
    AND mc.IsDeleted = 0";


                using (SqlCommand cmd =
                    new SqlCommand(query, con))
                {
                    cmd.Parameters.Add(
                        "@CampId",
                        SqlDbType.BigInt).Value =
                        model.CampId;


                    using (SqlDataReader dr =
                        cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            model.CampTitle =
                                dr["CampTitle"].ToString();

                            model.CampImage =
                                dr["CampImage"] == DBNull.Value
                                ? ""
                                : dr["CampImage"].ToString();

                            model.CampBanner =
                                dr["CampBanner"] == DBNull.Value
                                ? ""
                                : dr["CampBanner"].ToString();

                            model.CampDescription =
                                dr["CampDescription"] == DBNull.Value
                                ? ""
                                : dr["CampDescription"].ToString();

                            if (dr["CampDate"] != DBNull.Value)
                            {
                                model.CampDate =
                                    Convert.ToDateTime(
                                        dr["CampDate"]);
                            }

                            if (dr["StartTime"] != DBNull.Value)
                            {
                                model.StartTime =
                                    (TimeSpan)dr["StartTime"];
                            }

                            if (dr["EndTime"] != DBNull.Value)
                            {
                                model.EndTime =
                                    (TimeSpan)dr["EndTime"];
                            }

                            model.Venue =
                                dr["Venue"] == DBNull.Value
                                ? ""
                                : dr["Venue"].ToString();

                            model.Organizer =
                                dr["Organizer"] == DBNull.Value
                                ? ""
                                : dr["Organizer"].ToString();

                            model.DoctorName =
                                dr["DoctorName"].ToString();

                            model.DepartmentName =
                                dr["DepartmentName"].ToString();

                            model.RegistrationFee =
                                Convert.ToDecimal(
                                    dr["RegistrationFee"]);

                            model.MaxParticipants =
                                Convert.ToInt32(
                                    dr["MaxParticipants"]);

                            model.AvailableSeats =
                                Convert.ToInt32(
                                    dr["AvailableSeats"]);

                            model.ContactNumber =
                                dr["ContactNumber"] == DBNull.Value
                                ? ""
                                : dr["ContactNumber"].ToString();

                            model.CampEmail =
                                dr["Email"] == DBNull.Value
                                ? ""
                                : dr["Email"].ToString();

                            model.Benefits =
                                dr["Benefits"] == DBNull.Value
                                ? ""
                                : dr["Benefits"].ToString();

                            model.Instructions =
                                dr["Instructions"] == DBNull.Value
                                ? ""
                                : dr["Instructions"].ToString();
                        }
                    }
                }
            }


            ViewBag.GenderList =
                new List<SelectListItem>
                {
            new SelectListItem
            {
                Text = "Select Gender",
                Value = ""
            },

            new SelectListItem
            {
                Text = "Male",
                Value = "Male"
            },

            new SelectListItem
            {
                Text = "Female",
                Value = "Female"
            },

            new SelectListItem
            {
                Text = "Other",
                Value = "Other"
            }
                };
        }

        //======================================================
        // MY CAMP REGISTRATIONS
        //======================================================
        [HttpGet]
        public IActionResult MyCampRegistrations()
        {
            string customerSession =
                HttpContext.Session.GetString("CustomerId");

            if (string.IsNullOrEmpty(customerSession))
            {
                return RedirectToAction("Login");
            }

            long customerId =
                Convert.ToInt64(customerSession);


            List<CampRegistrationModel> list =
                new List<CampRegistrationModel>();


            try
            {
                using (SqlConnection con =
                    new SqlConnection(cs))
                {
                    con.Open();


                    string query = @"
SELECT
    cr.CampRegistrationId,
    cr.CampId,
    cr.CustomerId,
    cr.RegistrationNo,
    cr.RegistrationDate,
    cr.ParticipantName,
    cr.MobileNo,
    cr.Email,
    cr.Age,
    cr.Gender,
    cr.HealthConcern,
    cr.RegistrationStatus,
    cr.PaymentStatus,
    cr.Amount,
    cr.AdminRemark,
    cr.IsActive,
    cr.IsDeleted,
    cr.CreatedDate,
    cr.UpdatedDate,

    mc.CampTitle,
    mc.CampImage,
    mc.CampDate,
    mc.StartTime,
    mc.EndTime,
    mc.Venue,

    d.DoctorName,

    dep.DepartmentName,

    mc.RegistrationFee,
    mc.MaxParticipants,
    mc.AvailableSeats

FROM tbl_CampRegistration cr

INNER JOIN tbl_MedicalCamp mc
    ON cr.CampId = mc.CampId

INNER JOIN tbl_Doctor d
    ON mc.DoctorId = d.DoctorId

INNER JOIN tbl_Department dep
    ON mc.DepartmentId = dep.DepartmentId

WHERE
    cr.CustomerId = @CustomerId
    AND cr.IsDeleted = 0

ORDER BY
    cr.RegistrationDate DESC";


                    using (SqlCommand cmd =
                        new SqlCommand(query, con))
                    {
                        cmd.Parameters.Add(
                            "@CustomerId",
                            SqlDbType.BigInt).Value =
                            customerId;


                        using (SqlDataReader dr =
                            cmd.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                CampRegistrationModel item =
                                    new CampRegistrationModel();


                                item.CampRegistrationId =
                                    Convert.ToInt64(
                                        dr["CampRegistrationId"]);


                                item.CampId =
                                    Convert.ToInt64(
                                        dr["CampId"]);


                                item.CustomerId =
                                    Convert.ToInt64(
                                        dr["CustomerId"]);


                                item.RegistrationNo =
                                    dr["RegistrationNo"]?.ToString();


                                if (dr["RegistrationDate"] != DBNull.Value)
                                {
                                    item.RegistrationDate =
                                        Convert.ToDateTime(
                                            dr["RegistrationDate"]);
                                }


                                item.ParticipantName =
                                    dr["ParticipantName"] ==
                                    DBNull.Value
                                    ? ""
                                    : dr["ParticipantName"].ToString();


                                item.MobileNo =
                                    dr["MobileNo"] ==
                                    DBNull.Value
                                    ? ""
                                    : dr["MobileNo"].ToString();


                                item.Email =
                                    dr["Email"] ==
                                    DBNull.Value
                                    ? ""
                                    : dr["Email"].ToString();


                                if (dr["Age"] != DBNull.Value)
                                {
                                    item.Age =
                                        Convert.ToInt32(
                                            dr["Age"]);
                                }


                                item.Gender =
                                    dr["Gender"] ==
                                    DBNull.Value
                                    ? ""
                                    : dr["Gender"].ToString();


                                item.HealthConcern =
                                    dr["HealthConcern"] ==
                                    DBNull.Value
                                    ? ""
                                    : dr["HealthConcern"].ToString();


                                item.RegistrationStatus =
                                    dr["RegistrationStatus"] ==
                                    DBNull.Value
                                    ? ""
                                    : dr["RegistrationStatus"].ToString();


                                item.PaymentStatus =
                                    dr["PaymentStatus"] ==
                                    DBNull.Value
                                    ? ""
                                    : dr["PaymentStatus"].ToString();


                                item.Amount =
                                    dr["Amount"] ==
                                    DBNull.Value
                                    ? 0
                                    : Convert.ToDecimal(
                                        dr["Amount"]);


                                item.AdminRemark =
                                    dr["AdminRemark"] ==
                                    DBNull.Value
                                    ? ""
                                    : dr["AdminRemark"].ToString();


                                item.IsActive =
                                    Convert.ToBoolean(
                                        dr["IsActive"]);


                                item.IsDeleted =
                                    Convert.ToBoolean(
                                        dr["IsDeleted"]);


                                if (dr["CreatedDate"] != DBNull.Value)
                                {
                                    item.CreatedDate =
                                        Convert.ToDateTime(
                                            dr["CreatedDate"]);
                                }


                                if (dr["UpdatedDate"] != DBNull.Value)
                                {
                                    item.UpdatedDate =
                                        Convert.ToDateTime(
                                            dr["UpdatedDate"]);
                                }


                                item.CampTitle =
                                    dr["CampTitle"] ==
                                    DBNull.Value
                                    ? ""
                                    : dr["CampTitle"].ToString();


                                item.CampImage =
                                    dr["CampImage"] ==
                                    DBNull.Value
                                    ? ""
                                    : dr["CampImage"].ToString();


                                if (dr["CampDate"] != DBNull.Value)
                                {
                                    item.CampDate =
                                        Convert.ToDateTime(
                                            dr["CampDate"]);
                                }


                                if (dr["StartTime"] != DBNull.Value)
                                {
                                    item.StartTime =
                                        (TimeSpan)
                                        dr["StartTime"];
                                }


                                if (dr["EndTime"] != DBNull.Value)
                                {
                                    item.EndTime =
                                        (TimeSpan)
                                        dr["EndTime"];
                                }


                                item.Venue =
                                    dr["Venue"] ==
                                    DBNull.Value
                                    ? ""
                                    : dr["Venue"].ToString();


                                item.DoctorName =
                                    dr["DoctorName"] ==
                                    DBNull.Value
                                    ? ""
                                    : dr["DoctorName"].ToString();


                                item.DepartmentName =
                                    dr["DepartmentName"] ==
                                    DBNull.Value
                                    ? ""
                                    : dr["DepartmentName"].ToString();


                                item.RegistrationFee =
                                    dr["RegistrationFee"] ==
                                    DBNull.Value
                                    ? 0
                                    : Convert.ToDecimal(
                                        dr["RegistrationFee"]);


                                item.MaxParticipants =
                                    dr["MaxParticipants"] ==
                                    DBNull.Value
                                    ? 0
                                    : Convert.ToInt32(
                                        dr["MaxParticipants"]);


                                item.AvailableSeats =
                                    dr["AvailableSeats"] ==
                                    DBNull.Value
                                    ? 0
                                    : Convert.ToInt32(
                                        dr["AvailableSeats"]);


                                list.Add(item);
                            }
                        }
                    }
                }


                return View(list);
            }
            catch (Exception ex)
            {
                TempData["Error"] =
                    "Unable to load registrations: " +
                    ex.Message;

                return View(list);
            }
        }

        [HttpGet]
        public IActionResult CampRegistrationDetails(long id)
        {

            if (HttpContext.Session.GetString("CustomerId") == null)
            {
                return RedirectToAction("Login");
            }

            long customerId =
                Convert.ToInt64(
                    HttpContext.Session.GetString("CustomerId"));

            CampRegistrationModel model =
                new CampRegistrationModel();

            try
            {
                using (SqlConnection con = new SqlConnection(cs))
                {
                    con.Open();


                    string query = @"
                SELECT

                    cr.CampRegistrationId,
                    cr.CampId,
                    cr.CustomerId,
                    cr.RegistrationNo,
                    cr.RegistrationDate,

                    cr.ParticipantName,
                    cr.MobileNo,
                    cr.Email,
                    cr.Age,
                    cr.Gender,
                    cr.HealthConcern,

                    cr.RegistrationStatus,
                    cr.PaymentStatus,
                    cr.Amount,
                    cr.AdminRemark,

                    cr.IsActive,
                    cr.IsDeleted,

                    cr.CreatedDate,
                    cr.UpdatedDate,

                    mc.CampTitle,
                    mc.CampImage,
                    mc.CampBanner,
                    mc.CampDescription,
                    mc.CampDate,
                    mc.StartTime,
                    mc.EndTime,
                    mc.Venue,
                    mc.Organizer,
                    mc.Benefits,
                    mc.Instructions,
                    mc.ContactNumber,
                    mc.Email AS CampEmail,

                    d.DoctorName,
                    dep.DepartmentName,

                    mc.RegistrationFee,
                    mc.MaxParticipants,
                    mc.AvailableSeats

                FROM tbl_CampRegistration cr

                INNER JOIN tbl_MedicalCamp mc
                    ON cr.CampId = mc.CampId

                INNER JOIN tbl_Doctor d
                    ON mc.DoctorId = d.DoctorId

                INNER JOIN tbl_Department dep
                    ON mc.DepartmentId = dep.DepartmentId

                WHERE
                    cr.CampRegistrationId = @CampRegistrationId
                    AND cr.CustomerId = @CustomerId
                    AND cr.IsDeleted = 0";

                    SqlCommand cmd =
                        new SqlCommand(query, con);

                    cmd.Parameters.AddWithValue(
                        "@CampRegistrationId",
                        id);

                    cmd.Parameters.AddWithValue(
                        "@CustomerId",
                        customerId);

                    SqlDataReader dr =
                        cmd.ExecuteReader();


                    if (dr.Read())
                    {

                        model.CampRegistrationId =
                            Convert.ToInt64(
                                dr["CampRegistrationId"]);

                        model.CampId =
                            Convert.ToInt64(
                                dr["CampId"]);

                        model.CustomerId =
                            Convert.ToInt64(
                                dr["CustomerId"]);

                        model.RegistrationNo =
                            dr["RegistrationNo"] == DBNull.Value
                                ? ""
                                : dr["RegistrationNo"].ToString();

                        if (dr["RegistrationDate"] != DBNull.Value)
                        {
                            model.RegistrationDate =
                                Convert.ToDateTime(
                                    dr["RegistrationDate"]);
                        }


                        model.ParticipantName =
                            dr["ParticipantName"] == DBNull.Value
                                ? ""
                                : dr["ParticipantName"].ToString();

                        model.MobileNo =
                            dr["MobileNo"] == DBNull.Value
                                ? ""
                                : dr["MobileNo"].ToString();

                        model.Email =
                            dr["Email"] == DBNull.Value
                                ? ""
                                : dr["Email"].ToString();

                        if (dr["Age"] != DBNull.Value)
                        {
                            model.Age =
                                Convert.ToInt32(
                                    dr["Age"]);
                        }

                        model.Gender =
                            dr["Gender"] == DBNull.Value
                                ? ""
                                : dr["Gender"].ToString();

                        model.HealthConcern =
                            dr["HealthConcern"] == DBNull.Value
                                ? ""
                                : dr["HealthConcern"].ToString();


                        model.RegistrationStatus =
                            dr["RegistrationStatus"] == DBNull.Value
                                ? ""
                                : dr["RegistrationStatus"].ToString();

                        model.PaymentStatus =
                            dr["PaymentStatus"] == DBNull.Value
                                ? ""
                                : dr["PaymentStatus"].ToString();

                        model.Amount =
                            Convert.ToDecimal(
                                dr["Amount"]);

                        model.AdminRemark =
                            dr["AdminRemark"] == DBNull.Value
                                ? ""
                                : dr["AdminRemark"].ToString();


                        model.CampTitle =
                            dr["CampTitle"] == DBNull.Value
                                ? ""
                                : dr["CampTitle"].ToString();

                        model.CampImage =
                            dr["CampImage"] == DBNull.Value
                                ? ""
                                : dr["CampImage"].ToString();

                      

                        model.CampDescription =
                            dr["CampDescription"] == DBNull.Value
                                ? ""
                                : dr["CampDescription"].ToString();

                        if (dr["CampDate"] != DBNull.Value)
                        {
                            model.CampDate =
                                Convert.ToDateTime(
                                    dr["CampDate"]);
                        }

                        if (dr["StartTime"] != DBNull.Value)
                        {
                            model.StartTime =
                                (TimeSpan)dr["StartTime"];
                        }

                        if (dr["EndTime"] != DBNull.Value)
                        {
                            model.EndTime =
                                (TimeSpan)dr["EndTime"];
                        }

                        model.Venue =
                            dr["Venue"] == DBNull.Value
                                ? ""
                                : dr["Venue"].ToString();

                        model.Organizer =
                            dr["Organizer"] == DBNull.Value
                                ? ""
                                : dr["Organizer"].ToString();


                        model.DoctorName =
                            dr["DoctorName"] == DBNull.Value
                                ? ""
                                : dr["DoctorName"].ToString();

                        model.DepartmentName =
                            dr["DepartmentName"] == DBNull.Value
                                ? ""
                                : dr["DepartmentName"].ToString();


                        model.RegistrationFee =
                            Convert.ToDecimal(
                                dr["RegistrationFee"]);

                        model.MaxParticipants =
                            Convert.ToInt32(
                                dr["MaxParticipants"]);

                        model.AvailableSeats =
                            Convert.ToInt32(
                                dr["AvailableSeats"]);


                        model.IsActive =
                            Convert.ToBoolean(
                                dr["IsActive"]);

                        model.IsDeleted =
                            Convert.ToBoolean(
                                dr["IsDeleted"]);


                        if (dr["CreatedDate"] != DBNull.Value)
                        {
                            model.CreatedDate =
                                Convert.ToDateTime(
                                    dr["CreatedDate"]);
                        }

                        if (dr["UpdatedDate"] != DBNull.Value)
                        {
                            model.UpdatedDate =
                                Convert.ToDateTime(
                                    dr["UpdatedDate"]);
                        }
                    }
                    else
                    {
                        dr.Close();

                        TempData["Error"] =
                            "Registration not found or you are not authorized to view it.";

                        return RedirectToAction(
                            "MyCampRegistrations");
                    }

                    dr.Close();
                }


                return View(model);
            }
            catch (Exception ex)
            {
                TempData["Error"] =
                    "Unable to load registration details: "
                    + ex.Message;

                return RedirectToAction(
                    "MyCampRegistrations");
            }
        }
      
        [HttpGet]
        public IActionResult CancelCampRegistration(long id)
        {

            if (HttpContext.Session.GetString("CustomerId") == null)
            {
                return RedirectToAction("Login");
            }

            long customerId =
                Convert.ToInt64(
                    HttpContext.Session.GetString("CustomerId"));


            try
            {
                using (SqlConnection con = new SqlConnection(cs))
                {
                    con.Open();


                    using (SqlTransaction transaction =
                           con.BeginTransaction())
                    {
                        try
                        {

                            string registrationQuery = @"
                        SELECT
                            cr.CampRegistrationId,
                            cr.CampId,
                            cr.RegistrationNo,
                            cr.RegistrationStatus,
                            cr.IsDeleted,

                            mc.CampDate,
                            mc.AvailableSeats,
                            mc.MaxParticipants

                        FROM tbl_CampRegistration cr

                        INNER JOIN tbl_MedicalCamp mc
                            ON cr.CampId = mc.CampId

                        WHERE
                            cr.CampRegistrationId = @CampRegistrationId
                            AND cr.CustomerId = @CustomerId
                            AND cr.IsDeleted = 0

                        FOR UPDATE";


                            SqlCommand registrationCmd =
                                new SqlCommand(
                                    registrationQuery,
                                    con,
                                    transaction);

                            registrationCmd.Parameters.AddWithValue(
                                "@CampRegistrationId",
                                id);

                            registrationCmd.Parameters.AddWithValue(
                                "@CustomerId",
                                customerId);


                            SqlDataReader dr =
                                registrationCmd.ExecuteReader();


                            if (!dr.Read())
                            {
                                dr.Close();

                                transaction.Rollback();

                                TempData["Error"] =
                                    "Registration not found or you are not authorized to cancel it.";

                                return RedirectToAction(
                                    "MyCampRegistrations");
                            }


                            long campId =
                                Convert.ToInt64(
                                    dr["CampId"]);


                            string registrationNo =
                                dr["RegistrationNo"] == DBNull.Value
                                    ? ""
                                    : dr["RegistrationNo"].ToString();


                            string registrationStatus =
                                dr["RegistrationStatus"] == DBNull.Value
                                    ? ""
                                    : dr["RegistrationStatus"].ToString();


                            DateTime campDate =
                                Convert.ToDateTime(
                                    dr["CampDate"]);


                            int availableSeats =
                                Convert.ToInt32(
                                    dr["AvailableSeats"]);


                            int maxParticipants =
                                Convert.ToInt32(
                                    dr["MaxParticipants"]);


                            dr.Close();

                            if (registrationStatus != "Pending" &&
                                registrationStatus != "Confirmed")
                            {
                                transaction.Rollback();

                                TempData["Error"] =
                                    "This registration cannot be cancelled because its current status is "
                                    + registrationStatus + ".";

                                return RedirectToAction(
                                    "MyCampRegistrations");
                            }

                            if (campDate.Date < DateTime.Today)
                            {
                                transaction.Rollback();

                                TempData["Error"] =
                                    "This medical camp has already taken place, so the registration cannot be cancelled.";

                                return RedirectToAction(
                                    "MyCampRegistrations");
                            }

                            string cancelQuery = @"
                        UPDATE tbl_CampRegistration
                        SET
                            RegistrationStatus = 'Cancelled',
                            IsActive = 0,
                            UpdatedDate = @UpdatedDate
                        WHERE
                            CampRegistrationId = @CampRegistrationId
                            AND CustomerId = @CustomerId
                            AND IsDeleted = 0
                            AND RegistrationStatus IN
                                ('Pending', 'Confirmed')";


                            SqlCommand cancelCmd =
                                new SqlCommand(
                                    cancelQuery,
                                    con,
                                    transaction);


                            cancelCmd.Parameters.AddWithValue(
                                "@UpdatedDate",
                                DateTime.Now);

                            cancelCmd.Parameters.AddWithValue(
                                "@CampRegistrationId",
                                id);

                            cancelCmd.Parameters.AddWithValue(
                                "@CustomerId",
                                customerId);


                            int cancelResult =
                                cancelCmd.ExecuteNonQuery();


                            if (cancelResult <= 0)
                            {
                                transaction.Rollback();

                                TempData["Error"] =
                                    "Unable to cancel the medical camp registration.";

                                return RedirectToAction(
                                    "MyCampRegistrations");
                            }

                            string updateCampQuery = @"
                        UPDATE tbl_MedicalCamp
                        SET
                            AvailableSeats =
                                CASE
                                    WHEN AvailableSeats < MaxParticipants
                                    THEN AvailableSeats + 1
                                    ELSE MaxParticipants
                                END,
                            UpdatedDate = @UpdatedDate
                        WHERE
                            CampId = @CampId
                            AND IsDeleted = 0";


                            SqlCommand updateCampCmd =
                                new SqlCommand(
                                    updateCampQuery,
                                    con,
                                    transaction);


                            updateCampCmd.Parameters.AddWithValue(
                                "@UpdatedDate",
                                DateTime.Now);

                            updateCampCmd.Parameters.AddWithValue(
                                "@CampId",
                                campId);


                            int updateResult =
                                updateCampCmd.ExecuteNonQuery();

                            if (updateResult <= 0)
                            {
                                transaction.Rollback();

                                TempData["Error"] =
                                    "Registration was not cancelled because the camp seat could not be updated.";

                                return RedirectToAction(
                                    "MyCampRegistrations");
                            }

                            transaction.Commit();

                            TempData["Success"] =
                                "Medical Camp registration " +
                                registrationNo +
                                " has been cancelled successfully.";


                            return RedirectToAction(
                                "MyCampRegistrations");
                        }
                        catch
                        {
                            try
                            {
                                transaction.Rollback();
                            }
                            catch
                            {
                            }

                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] =
                    "Unable to cancel registration: " +
                    ex.Message;

                return RedirectToAction(
                    "MyCampRegistrations");
            }
        }
    
        [HttpGet]
        public IActionResult PrintCampRegistration(long id)
        {

            if (HttpContext.Session.GetString("CustomerId") == null)
            {
                return RedirectToAction("Login");
            }

            long customerId =
                Convert.ToInt64(
                    HttpContext.Session.GetString("CustomerId"));

            CampRegistrationModel model =
                new CampRegistrationModel();

            try
            {
                using (SqlConnection con = new SqlConnection(cs))
                {
                    con.Open();


                    string query = @"
                SELECT

                    cr.CampRegistrationId,
                    cr.CampId,
                    cr.CustomerId,
                    cr.RegistrationNo,
                    cr.RegistrationDate,

                    cr.ParticipantName,
                    cr.MobileNo,
                    cr.Email,
                    cr.Age,
                    cr.Gender,
                    cr.HealthConcern,

                    cr.RegistrationStatus,
                    cr.PaymentStatus,
                    cr.Amount,
                    cr.AdminRemark,

                    mc.CampTitle,
                    mc.CampImage,
                    mc.CampBanner,
                    mc.CampDescription,
                    mc.CampDate,
                    mc.StartTime,
                    mc.EndTime,
                    mc.Venue,
                    mc.Organizer,
                    mc.Benefits,
                    mc.Instructions,
                    mc.ContactNumber,
                    mc.Email AS CampEmail,

                    d.DoctorName,

                    dep.DepartmentName,

                    mc.RegistrationFee,
                    mc.MaxParticipants,
                    mc.AvailableSeats

                FROM tbl_CampRegistration cr

                INNER JOIN tbl_MedicalCamp mc
                    ON cr.CampId = mc.CampId

                INNER JOIN tbl_Doctor d
                    ON mc.DoctorId = d.DoctorId

                INNER JOIN tbl_Department dep
                    ON mc.DepartmentId = dep.DepartmentId

                WHERE
                    cr.CampRegistrationId = @CampRegistrationId
                    AND cr.CustomerId = @CustomerId
                    AND cr.IsDeleted = 0";

                    SqlCommand cmd =
                        new SqlCommand(query, con);

                    cmd.Parameters.AddWithValue(
                        "@CampRegistrationId",
                        id);

                    cmd.Parameters.AddWithValue(
                        "@CustomerId",
                        customerId);

                    SqlDataReader dr =
                        cmd.ExecuteReader();


                    if (dr.Read())
                    {

                        model.CampRegistrationId =
                            Convert.ToInt64(
                                dr["CampRegistrationId"]);

                        model.CampId =
                            Convert.ToInt64(
                                dr["CampId"]);

                        model.CustomerId =
                            Convert.ToInt64(
                                dr["CustomerId"]);

                        model.RegistrationNo =
                            dr["RegistrationNo"] == DBNull.Value
                                ? ""
                                : dr["RegistrationNo"].ToString();

                        if (dr["RegistrationDate"] != DBNull.Value)
                        {
                            model.RegistrationDate =
                                Convert.ToDateTime(
                                    dr["RegistrationDate"]);
                        }


                        model.ParticipantName =
                            dr["ParticipantName"] == DBNull.Value
                                ? ""
                                : dr["ParticipantName"].ToString();

                        model.MobileNo =
                            dr["MobileNo"] == DBNull.Value
                                ? ""
                                : dr["MobileNo"].ToString();

                        model.Email =
                            dr["Email"] == DBNull.Value
                                ? ""
                                : dr["Email"].ToString();

                        if (dr["Age"] != DBNull.Value)
                        {
                            model.Age =
                                Convert.ToInt32(
                                    dr["Age"]);
                        }

                        model.Gender =
                            dr["Gender"] == DBNull.Value
                                ? ""
                                : dr["Gender"].ToString();

                        model.HealthConcern =
                            dr["HealthConcern"] == DBNull.Value
                                ? ""
                                : dr["HealthConcern"].ToString();


                        model.RegistrationStatus =
                            dr["RegistrationStatus"] == DBNull.Value
                                ? ""
                                : dr["RegistrationStatus"].ToString();

                        model.PaymentStatus =
                            dr["PaymentStatus"] == DBNull.Value
                                ? ""
                                : dr["PaymentStatus"].ToString();

                        model.Amount =
                            Convert.ToDecimal(
                                dr["Amount"]);

                        model.AdminRemark =
                            dr["AdminRemark"] == DBNull.Value
                                ? ""
                                : dr["AdminRemark"].ToString();


                        model.CampTitle =
                            dr["CampTitle"] == DBNull.Value
                                ? ""
                                : dr["CampTitle"].ToString();

                        model.CampImage =
                            dr["CampImage"] == DBNull.Value
                                ? ""
                                : dr["CampImage"].ToString();

                        model.CampBanner =
                            dr["CampBanner"] == DBNull.Value
                                ? ""
                                : dr["CampBanner"].ToString();

                        model.CampDescription =
                            dr["CampDescription"] == DBNull.Value
                                ? ""
                                : dr["CampDescription"].ToString();

                        if (dr["CampDate"] != DBNull.Value)
                        {
                            model.CampDate =
                                Convert.ToDateTime(
                                    dr["CampDate"]);
                        }

                        if (dr["StartTime"] != DBNull.Value)
                        {
                            model.StartTime =
                                (TimeSpan)dr["StartTime"];
                        }

                        if (dr["EndTime"] != DBNull.Value)
                        {
                            model.EndTime =
                                (TimeSpan)dr["EndTime"];
                        }

                        model.Venue =
                            dr["Venue"] == DBNull.Value
                                ? ""
                                : dr["Venue"].ToString();

                        model.Organizer =
                            dr["Organizer"] == DBNull.Value
                                ? ""
                                : dr["Organizer"].ToString();

                        model.Benefits =
                            dr["Benefits"] == DBNull.Value
                                ? ""
                                : dr["Benefits"].ToString();

                        model.Instructions =
                            dr["Instructions"] == DBNull.Value
                                ? ""
                                : dr["Instructions"].ToString();

                        model.ContactNumber =
                            dr["ContactNumber"] == DBNull.Value
                                ? ""
                                : dr["ContactNumber"].ToString();

                        model.CampEmail =
                            dr["CampEmail"] == DBNull.Value
                                ? ""
                                : dr["CampEmail"].ToString();



                        model.DoctorName =
                            dr["DoctorName"] == DBNull.Value
                                ? ""
                                : dr["DoctorName"].ToString();

                        model.DepartmentName =
                            dr["DepartmentName"] == DBNull.Value
                                ? ""
                                : dr["DepartmentName"].ToString();


                        model.RegistrationFee =
                            Convert.ToDecimal(
                                dr["RegistrationFee"]);

                        model.MaxParticipants =
                            Convert.ToInt32(
                                dr["MaxParticipants"]);

                        model.AvailableSeats =
                            Convert.ToInt32(
                                dr["AvailableSeats"]);
                    }
                    else
                    {
                        dr.Close();

                        TempData["Error"] =
                            "Registration not found or you are not authorized to print it.";

                        return RedirectToAction(
                            "MyCampRegistrations");
                    }

                    dr.Close();
                }


                return View("PrintCampRegistration", model);
            }
            catch (Exception ex)
            {
                TempData["Error"] =
                    "Unable to prepare registration print: "
                    + ex.Message;

                return RedirectToAction(
                    "MyCampRegistrations");
            }
        }
        
        [HttpGet]
        

        [HttpGet]
        public IActionResult LabTests(string search = "", string category = "", decimal? maxPrice = null, string sort = "")
        {
            List<LabTestModel> list = new List<LabTestModel>();

            using (SqlConnection con = new SqlConnection(cs))
            {
                string query = @"
                    SELECT * FROM tbl_LabTest
                    WHERE IsDeleted = 0 AND IsActive = 1
                    AND (@Search = '' OR TestName LIKE '%' + @Search + '%' OR Description LIKE '%' + @Search + '%' OR TestCategory LIKE '%' + @Search + '%')
                    AND (@Category = '' OR TestCategory = @Category)
                    AND (@MaxPrice IS NULL OR Price <= @MaxPrice)";

                if (sort == "price_asc")
                {
                    query += " ORDER BY Price ASC";
                }
                else if (sort == "price_desc")
                {
                    query += " ORDER BY Price DESC";
                }
                else if (sort == "name")
                {
                    query += " ORDER BY TestName ASC";
                }
                else
                {
                    query += " ORDER BY LabTestId DESC";
                }

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@Search", search ?? "");
                cmd.Parameters.AddWithValue("@Category", category ?? "");
                cmd.Parameters.AddWithValue("@MaxPrice", (object)maxPrice ?? DBNull.Value);

                con.Open();
                SqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    LabTestModel item = new LabTestModel
                    {
                        LabTestId = Convert.ToInt64(dr["LabTestId"]),
                        TestName = dr["TestName"].ToString(),
                        TestCategory = dr["TestCategory"] != DBNull.Value ? dr["TestCategory"].ToString() : "",
                        Price = Convert.ToDecimal(dr["Price"]),
                        Description = dr["Description"] != DBNull.Value ? dr["Description"].ToString() : "",
                        SampleType = dr["SampleType"] != DBNull.Value ? dr["SampleType"].ToString() : "",
                        PreparationInstructions = dr["PreparationInstructions"] != DBNull.Value ? dr["PreparationInstructions"].ToString() : "",
                        ReportTime = dr["ReportTime"] != DBNull.Value ? dr["ReportTime"].ToString() : "",
                        IsActive = Convert.ToBoolean(dr["IsActive"]),
                        IsDeleted = Convert.ToBoolean(dr["IsDeleted"]),
                        CreatedDate = Convert.ToDateTime(dr["CreatedDate"]),
                        UpdatedDate = dr["UpdatedDate"] != DBNull.Value ? Convert.ToDateTime(dr["UpdatedDate"]) : null
                    };
                    list.Add(item);
                }
            }

            ViewBag.Search = search;
            ViewBag.Category = category;
            ViewBag.MaxPrice = maxPrice;
            ViewBag.Sort = sort;

            return View("~/Views/Customer/LabTests.cshtml", list);
        }

        [HttpGet]
        public IActionResult LabTestDetails(long id)
        {
            LabTestModel model = new LabTestModel();

            using (SqlConnection con = new SqlConnection(cs))
            {
                SqlCommand cmd = new SqlCommand("SELECT * FROM tbl_LabTest WHERE LabTestId = @LabTestId AND IsDeleted = 0", con);
                cmd.Parameters.AddWithValue("@LabTestId", id);

                con.Open();
                SqlDataReader dr = cmd.ExecuteReader();
                if (dr.Read())
                {
                    model.LabTestId = Convert.ToInt64(dr["LabTestId"]);
                    model.TestName = dr["TestName"].ToString();
                    model.TestCategory = dr["TestCategory"] != DBNull.Value ? dr["TestCategory"].ToString() : "";
                    model.Price = Convert.ToDecimal(dr["Price"]);
                    model.Description = dr["Description"] != DBNull.Value ? dr["Description"].ToString() : "";
                    model.SampleType = dr["SampleType"] != DBNull.Value ? dr["SampleType"].ToString() : "";
                    model.PreparationInstructions = dr["PreparationInstructions"] != DBNull.Value ? dr["PreparationInstructions"].ToString() : "";
                    model.ReportTime = dr["ReportTime"] != DBNull.Value ? dr["ReportTime"].ToString() : "";
                    model.IsActive = Convert.ToBoolean(dr["IsActive"]);
                    model.IsDeleted = Convert.ToBoolean(dr["IsDeleted"]);
                    model.CreatedDate = Convert.ToDateTime(dr["CreatedDate"]);
                    model.UpdatedDate = dr["UpdatedDate"] != DBNull.Value ? Convert.ToDateTime(dr["UpdatedDate"]) : null;
                }
                else
                {
                    TempData["Error"] = "Lab Test not found.";
                    return RedirectToAction("LabTests");
                }
            }

            return View("~/Views/Customer/LabTestDetails.cshtml", model);
        }
        [HttpGet]
        public IActionResult BookLabTest(long id)
        {
            if (HttpContext.Session.GetString("CustomerId") == null)
            {
                TempData["Error"] = "Please sign in to book a lab test.";
                return RedirectToAction("Login");
            }

            long customerId = Convert.ToInt64(
                HttpContext.Session.GetString("CustomerId"));

            LabBookingModel model = new LabBookingModel();

            try
            {
                using (SqlConnection con = new SqlConnection(cs))
                {
                    con.Open();


                    string testQuery = @"
                SELECT
                    LabTestId,
                    TestName,
                    TestCategory,
                    SampleType,
                    ReportTime,
                    Price
                FROM tbl_LabTest
                WHERE
                    LabTestId = @LabTestId
                    AND IsActive = 1
                    AND IsDeleted = 0";

                    using (SqlCommand testCmd =
                           new SqlCommand(testQuery, con))
                    {
                        testCmd.Parameters.Add(
                            "@LabTestId",
                            SqlDbType.BigInt).Value = id;

                        using (SqlDataReader testDr =
                               testCmd.ExecuteReader())
                        {
                            if (!testDr.Read())
                            {
                                TempData["Error"] =
                                    "Selected Lab Test is not available.";

                                return RedirectToAction("LabTests");
                            }

                            model.LabTestId =
                                Convert.ToInt64(
                                    testDr["LabTestId"]);

                            model.TestName =
                                testDr["TestName"] == DBNull.Value
                                    ? ""
                                    : testDr["TestName"].ToString();

                            model.TestCategory =
                                testDr["TestCategory"] == DBNull.Value
                                    ? ""
                                    : testDr["TestCategory"].ToString();

                            model.SampleType =
                                testDr["SampleType"] == DBNull.Value
                                    ? ""
                                    : testDr["SampleType"].ToString();

                            model.ReportTime =
                                testDr["ReportTime"] == DBNull.Value
                                    ? ""
                                    : testDr["ReportTime"].ToString();

                            model.Amount =
                                testDr["Price"] == DBNull.Value
                                    ? 0
                                    : Convert.ToDecimal(
                                        testDr["Price"]);
                        }
                    }


                    string customerQuery = @"
                SELECT
                    FullName,
                    MobileNo,
                    Email,
                    Gender,
                    DateOfBirth
                FROM tbl_Customer
                WHERE
                    CustomerId = @CustomerId
                    AND IsActive = 1
                    AND IsDeleted = 0";

                    using (SqlCommand customerCmd =
                           new SqlCommand(customerQuery, con))
                    {
                        customerCmd.Parameters.Add(
                            "@CustomerId",
                            SqlDbType.BigInt).Value = customerId;

                        using (SqlDataReader customerDr =
                               customerCmd.ExecuteReader())
                        {
                            if (!customerDr.Read())
                            {
                                TempData["Error"] =
                                    "Customer information could not be found.";

                                return RedirectToAction("Login");
                            }

                            model.CustomerId = customerId;



                            model.PatientName =
                                customerDr["FullName"] == DBNull.Value
                                    ? ""
                                    : customerDr["FullName"].ToString();



                            model.MobileNo =
                                customerDr["MobileNo"] == DBNull.Value
                                    ? ""
                                    : customerDr["MobileNo"].ToString();


                        

                            model.Email =
                                customerDr["Email"] == DBNull.Value
                                    ? ""
                                    : customerDr["Email"].ToString();


                           

                            model.Gender =
                                customerDr["Gender"] == DBNull.Value
                                    ? ""
                                    : customerDr["Gender"].ToString();



                            if (customerDr["DateOfBirth"] != DBNull.Value)
                            {
                                DateTime dob =
                                    Convert.ToDateTime(
                                        customerDr["DateOfBirth"]);

                                DateTime today =
                                    DateTime.Today;

                                int age =
                                    today.Year - dob.Year;

                                if (dob.Date >
                                    today.AddYears(-age))
                                {
                                    age--;
                                }

                                if (age >= 0 && age <= 120)
                                {
                                    model.Age = age;
                                }
                            }
                        }
                    }
                }


                model.BookingDate = DateTime.Now;

                model.PreferredDate =
                    DateTime.Today.AddDays(1);

                model.TestStatus =
                    "Pending";

                model.PaymentStatus =
                    model.Amount > 0
                        ? "Pending"
                        : "NotRequired";

                model.IsActive = true;

                model.IsDeleted = false;


                ViewBag.GenderList =
                    new List<SelectListItem>
                    {
                new SelectListItem
                {
                    Text = "Select Gender",
                    Value = ""
                },

                new SelectListItem
                {
                    Text = "Male",
                    Value = "Male"
                },

                new SelectListItem
                {
                    Text = "Female",
                    Value = "Female"
                },

                new SelectListItem
                {
                    Text = "Other",
                    Value = "Other"
                }
                    };



                return View(
                    "~/Views/Customer/BookLabTest.cshtml",
                    model);
            }
            catch (Exception ex)
            {
                TempData["Error"] =
                    "Unable to load lab test booking page: "
                    + ex.Message;

                return RedirectToAction("LabTests");
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult BookLabTest(LabBookingModel model)
        {
            string bookingNo = "";

            try
            {

                string customerSession =
                    HttpContext.Session.GetString("CustomerId");

                if (string.IsNullOrEmpty(customerSession))
                {
                    TempData["Error"] =
                        "Please sign in to book a lab test.";

                    return RedirectToAction("Login");
                }

                long customerId =
                    Convert.ToInt64(customerSession);

                model.CustomerId = customerId;


                ModelState.Clear();


                if (model.LabTestId <= 0)
                {
                    TempData["Error"] =
                        "Invalid lab test selected.";

                    return RedirectToAction("LabTests");
                }

                if (string.IsNullOrWhiteSpace(model.PatientName))
                {
                    TempData["Error"] =
                        "Patient name is required.";

                    return RedirectToAction(
                        "BookLabTest",
                        new { id = model.LabTestId });
                }

                if (string.IsNullOrWhiteSpace(model.MobileNo))
                {
                    TempData["Error"] =
                        "Mobile number is required.";

                    return RedirectToAction(
                        "BookLabTest",
                        new { id = model.LabTestId });
                }

                if (!model.PreferredDate.HasValue)
                {
                    TempData["Error"] =
                        "Preferred date is required.";

                    return RedirectToAction(
                        "BookLabTest",
                        new { id = model.LabTestId });
                }


                using (SqlConnection con =
                       new SqlConnection(cs))
                {
                    con.Open();


                    string testQuery = @"
                SELECT
                    LabTestId,
                    TestName,
                    TestCategory,
                    SampleType,
                    Price
                FROM tbl_LabTest
                WHERE
                    LabTestId = @LabTestId
                    AND IsActive = 1
                    AND IsDeleted = 0";


                    decimal amount = 0;


                    using (SqlCommand testCmd =
                           new SqlCommand(testQuery, con))
                    {
                        testCmd.Parameters.Add(
                            "@LabTestId",
                            SqlDbType.BigInt).Value =
                            model.LabTestId;


                        using (SqlDataReader dr =
                               testCmd.ExecuteReader())
                        {
                            if (!dr.Read())
                            {
                                TempData["Error"] =
                                    "Selected lab test is not available.";

                                return RedirectToAction("LabTests");
                            }


                            model.TestName =
                                dr["TestName"] == DBNull.Value
                                ? ""
                                : dr["TestName"].ToString();


                            model.TestCategory =
                                dr["TestCategory"] == DBNull.Value
                                ? ""
                                : dr["TestCategory"].ToString();


                            model.SampleType =
                                dr["SampleType"] == DBNull.Value
                                ? ""
                                : dr["SampleType"].ToString();


                            amount =
                                dr["Price"] == DBNull.Value
                                ? 0
                                : Convert.ToDecimal(
                                    dr["Price"]);
                        }
                    }


                 
                    model.Amount = amount;


                    string customerQuery = @"
                SELECT
                    CustomerId
                FROM tbl_Customer
                WHERE
                    CustomerId = @CustomerId
                    AND IsActive = 1
                    AND IsDeleted = 0";


                    using (SqlCommand customerCmd =
                           new SqlCommand(
                               customerQuery,
                               con))
                    {
                        customerCmd.Parameters.Add(
                            "@CustomerId",
                            SqlDbType.BigInt).Value =
                            customerId;


                        object customer =
                            customerCmd.ExecuteScalar();


                        if (customer == null)
                        {
                            TempData["Error"] =
                                "Customer account not found.";

                            return RedirectToAction("Login");
                        }
                    }

                    bookingNo =
                        "LBK" +
                        DateTime.Now.ToString(
                            "yyyyMMddHHmmssfff");


                    string paymentStatus =
                        amount > 0
                        ? "Pending"
                        : "NotRequired";


                    string insertQuery = @"
                INSERT INTO tbl_LabBooking
                (
                    LabTestId,
                    CustomerId,
                    BookingNo,
                    BookingDate,
                    PatientName,
                    MobileNo,
                    Email,
                    Age,
                    Gender,
                    PreferredDate,
                    PreferredTime,
                    HealthConcern,
                    TestStatus,
                    PaymentStatus,
                    Amount,
                    AdminRemark,
                    IsActive,
                    IsDeleted,
                    CreatedDate,
                    UpdatedDate
                )
                VALUES
                (
                    @LabTestId,
                    @CustomerId,
                    @BookingNo,
                    GETDATE(),
                    @PatientName,
                    @MobileNo,
                    @Email,
                    @Age,
                    @Gender,
                    @PreferredDate,
                    @PreferredTime,
                    @HealthConcern,
                    @TestStatus,
                    @PaymentStatus,
                    @Amount,
                    @AdminRemark,
                    @IsActive,
                    @IsDeleted,
                    GETDATE(),
                    NULL
                )";


                    using (SqlCommand cmd =
                           new SqlCommand(
                               insertQuery,
                               con))
                    {
                        cmd.Parameters.Add(
                            "@LabTestId",
                            SqlDbType.BigInt).Value =
                            model.LabTestId;


                        cmd.Parameters.Add(
                            "@CustomerId",
                            SqlDbType.BigInt).Value =
                            customerId;


                        cmd.Parameters.Add(
                            "@BookingNo",
                            SqlDbType.NVarChar, 100).Value =
                            bookingNo;


                        cmd.Parameters.Add(
                            "@PatientName",
                            SqlDbType.NVarChar, 200).Value =
                            model.PatientName.Trim();


                        cmd.Parameters.Add(
                            "@MobileNo",
                            SqlDbType.NVarChar, 50).Value =
                            model.MobileNo.Trim();


                        cmd.Parameters.Add(
                            "@Email",
                            SqlDbType.NVarChar, 200).Value =
                            string.IsNullOrWhiteSpace(model.Email)
                            ? (object)DBNull.Value
                            : model.Email.Trim();


                        cmd.Parameters.Add(
                            "@Age",
                            SqlDbType.Int).Value =
                            model.Age.HasValue
                            ? (object)model.Age.Value
                            : DBNull.Value;


                        cmd.Parameters.Add(
                            "@Gender",
                            SqlDbType.NVarChar, 50).Value =
                            string.IsNullOrWhiteSpace(model.Gender)
                            ? (object)DBNull.Value
                            : model.Gender.Trim();


                        cmd.Parameters.Add(
                            "@PreferredDate",
                            SqlDbType.Date).Value =
                            model.PreferredDate.Value.Date;


                        cmd.Parameters.Add(
                            "@PreferredTime",
                            SqlDbType.NVarChar, 50).Value =
                            string.IsNullOrWhiteSpace(model.PreferredTime)
                            ? (object)DBNull.Value
                            : model.PreferredTime.Trim();


                        cmd.Parameters.Add(
                            "@HealthConcern",
                            SqlDbType.NVarChar, -1).Value =
                            string.IsNullOrWhiteSpace(model.HealthConcern)
                            ? (object)DBNull.Value
                            : model.HealthConcern.Trim();


                        cmd.Parameters.Add(
                            "@TestStatus",
                            SqlDbType.NVarChar, 50).Value =
                            "Pending";


                        cmd.Parameters.Add(
                            "@PaymentStatus",
                            SqlDbType.NVarChar, 50).Value =
                            paymentStatus;


                        SqlParameter amountParameter =
                            cmd.Parameters.Add(
                                "@Amount",
                                SqlDbType.Decimal);

                        amountParameter.Precision = 18;
                        amountParameter.Scale = 2;
                        amountParameter.Value = amount;


                        cmd.Parameters.Add(
                            "@AdminRemark",
                            SqlDbType.NVarChar, -1).Value =
                            DBNull.Value;


                        cmd.Parameters.Add(
                            "@IsActive",
                            SqlDbType.Bit).Value =
                            true;


                        cmd.Parameters.Add(
                            "@IsDeleted",
                            SqlDbType.Bit).Value =
                            false;


                        int result =
                            cmd.ExecuteNonQuery();


                        if (result <= 0)
                        {
                            TempData["Error"] =
                                "Lab test booking was not saved.";

                            return RedirectToAction(
                                "BookLabTest",
                                new { id = model.LabTestId });
                        }
                    }
                }


                TempData["Success"] =
                    "Lab test booked successfully! Booking Reference: "
                    + bookingNo;


                return RedirectToAction(
                    "MyLabTests",
                    "Customer");
            }
            catch (Exception ex)
            {
                TempData["Error"] =
                    "Booking failed: " + ex.Message;

                return RedirectToAction(
                    "BookLabTest",
                    new { id = model.LabTestId });
            }
        }
        private void LoadLabTestForBooking(
    LabBookingModel model)
        {
            using (SqlConnection con =
                   new SqlConnection(cs))
            {
                string query = @"
            SELECT
                LabTestId,
                TestName,
                TestCategory,
                SampleType,
                ReportTime,
                Price
            FROM tbl_LabTest
            WHERE
                LabTestId = @LabTestId
                AND IsActive = 1
                AND IsDeleted = 0";

                using (SqlCommand cmd =
                       new SqlCommand(query, con))
                {
                    cmd.Parameters.Add(
                        "@LabTestId",
                        SqlDbType.BigInt).Value =
                        model.LabTestId;

                    con.Open();

                    using (SqlDataReader dr =
                           cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            model.TestName =
                                dr["TestName"].ToString();

                            model.TestCategory =
                                dr["TestCategory"] == DBNull.Value
                                ? ""
                                : dr["TestCategory"].ToString();

                            model.SampleType =
                                dr["SampleType"] == DBNull.Value
                                ? ""
                                : dr["SampleType"].ToString();

                            model.ReportTime =
                                dr["ReportTime"] == DBNull.Value
                                ? ""
                                : dr["ReportTime"].ToString();

                            model.Amount =
                                dr["Price"] == DBNull.Value
                                ? 0
                                : Convert.ToDecimal(
                                    dr["Price"]);
                        }
                    }
                }
            }
        }
        private void LoadGenderList()
        {
            ViewBag.GenderList =
                new List<SelectListItem>
                {
            new SelectListItem
            {
                Text = "Select Gender",
                Value = ""
            },
            new SelectListItem
            {
                Text = "Male",
                Value = "Male"
            },
            new SelectListItem
            {
                Text = "Female",
                Value = "Female"
            },
            new SelectListItem
            {
                Text = "Other",
                Value = "Other"
            }
                };
        }

        [HttpGet]
        public IActionResult MyLabTests(string search = "", string status = "")
        {
            if (HttpContext.Session.GetString("CustomerId") == null)
            {
                return RedirectToAction("Login");
            }

            long customerId = Convert.ToInt64(HttpContext.Session.GetString("CustomerId"));
            List<LabBookingModel> list = new List<LabBookingModel>();

            using (SqlConnection con = new SqlConnection(cs))
            {
                string query = @"
                    SELECT B.*, T.TestName, T.TestCategory, T.SampleType
                    FROM tbl_LabBooking B
                    INNER JOIN tbl_LabTest T ON B.LabTestId = T.LabTestId
                    WHERE B.CustomerId = @CustomerId AND B.IsDeleted = 0
                    AND (@Search = '' OR B.BookingNo LIKE '%' + @Search + '%' OR B.PatientName LIKE '%' + @Search + '%' OR T.TestName LIKE '%' + @Search + '%')
                    AND (@Status = '' OR B.TestStatus = @Status)
                    ORDER BY B.LabBookingId DESC";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@CustomerId", customerId);
                cmd.Parameters.AddWithValue("@Search", search ?? "");
                cmd.Parameters.AddWithValue("@Status", status ?? "");

                con.Open();
                SqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    LabBookingModel model = new LabBookingModel
                    {
                        LabBookingId = Convert.ToInt64(dr["LabBookingId"]),
                        LabTestId = Convert.ToInt64(dr["LabTestId"]),
                        CustomerId = Convert.ToInt64(dr["CustomerId"]),
                        BookingNo = dr["BookingNo"].ToString(),
                        PatientName = dr["PatientName"].ToString(),
                        MobileNo = dr["MobileNo"] != DBNull.Value ? dr["MobileNo"].ToString() : "",
                        Email = dr["Email"] != DBNull.Value ? dr["Email"].ToString() : "",
                        Age = dr["Age"] != DBNull.Value ? Convert.ToInt32(dr["Age"]) : (int?)null,
                        Gender = dr["Gender"] != DBNull.Value ? dr["Gender"].ToString() : "",
                        PreferredDate = dr["PreferredDate"] != DBNull.Value ? Convert.ToDateTime(dr["PreferredDate"]) : (DateTime?)null,
                        PreferredTime = dr["PreferredTime"] != DBNull.Value ? dr["PreferredTime"].ToString() : "",
                        HealthConcern = dr["HealthConcern"] != DBNull.Value ? dr["HealthConcern"].ToString() : "",
                        Amount = Convert.ToDecimal(dr["Amount"]),
                        PaymentStatus = dr["PaymentStatus"].ToString(),
                        TestStatus = dr["TestStatus"].ToString(),
                        AdminRemark = dr["AdminRemark"] != DBNull.Value ? dr["AdminRemark"].ToString() : "",
                        BookingDate = Convert.ToDateTime(dr["BookingDate"]),
                        TestName = dr["TestName"] != DBNull.Value ? dr["TestName"].ToString() : "",
                        TestCategory = dr["TestCategory"] != DBNull.Value ? dr["TestCategory"].ToString() : "",
                        SampleType = dr["SampleType"] != DBNull.Value ? dr["SampleType"].ToString() : ""
                    };
                    list.Add(model);
                }
            }

            ViewBag.Search = search;
            ViewBag.Status = status;
            return View("~/Views/Customer/MyLabTests.cshtml", list);
        }

        [HttpGet]
        public IActionResult LabBookingDetails(long id)
        {
            if (HttpContext.Session.GetString("CustomerId") == null)
            {
                return RedirectToAction("Login");
            }

            long customerId = Convert.ToInt64(HttpContext.Session.GetString("CustomerId"));
            LabBookingModel model = new LabBookingModel();

            using (SqlConnection con = new SqlConnection(cs))
            {
                string query = @"
                    SELECT B.*, T.TestName, T.TestCategory, T.SampleType, T.ReportTime, T.PreparationInstructions, C.FullName AS CustomerName
                    FROM tbl_LabBooking B
                    INNER JOIN tbl_LabTest T ON B.LabTestId = T.LabTestId
                    LEFT JOIN tbl_Customer C ON B.CustomerId = C.CustomerId
                    WHERE B.LabBookingId = @LabBookingId AND B.CustomerId = @CustomerId AND B.IsDeleted = 0";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@LabBookingId", id);
                cmd.Parameters.AddWithValue("@CustomerId", customerId);

                con.Open();
                SqlDataReader dr = cmd.ExecuteReader();
                if (dr.Read())
                {
                    model.LabBookingId = Convert.ToInt64(dr["LabBookingId"]);
                    model.LabTestId = Convert.ToInt64(dr["LabTestId"]);
                    model.CustomerId = Convert.ToInt64(dr["CustomerId"]);
                    model.BookingNo = dr["BookingNo"].ToString();
                    model.PatientName = dr["PatientName"].ToString();
                    model.MobileNo = dr["MobileNo"] != DBNull.Value ? dr["MobileNo"].ToString() : "";
                    model.Email = dr["Email"] != DBNull.Value ? dr["Email"].ToString() : "";
                    model.Age = dr["Age"] != DBNull.Value ? Convert.ToInt32(dr["Age"]) : (int?)null;
                    model.Gender = dr["Gender"] != DBNull.Value ? dr["Gender"].ToString() : "";
                    model.PreferredDate = dr["PreferredDate"] != DBNull.Value ? Convert.ToDateTime(dr["PreferredDate"]) : (DateTime?)null;
                    model.PreferredTime = dr["PreferredTime"] != DBNull.Value ? dr["PreferredTime"].ToString() : "";
                    model.HealthConcern = dr["HealthConcern"] != DBNull.Value ? dr["HealthConcern"].ToString() : "";
                    model.Amount = Convert.ToDecimal(dr["Amount"]);
                    model.PaymentStatus = dr["PaymentStatus"].ToString();
                    model.TestStatus = dr["TestStatus"].ToString();
                    model.AdminRemark = dr["AdminRemark"] != DBNull.Value ? dr["AdminRemark"].ToString() : "";
                    model.BookingDate = Convert.ToDateTime(dr["BookingDate"]);
                    model.TestName = dr["TestName"] != DBNull.Value ? dr["TestName"].ToString() : "";
                    model.TestCategory = dr["TestCategory"] != DBNull.Value ? dr["TestCategory"].ToString() : "";
                    model.SampleType = dr["SampleType"] != DBNull.Value ? dr["SampleType"].ToString() : "";
                    model.CustomerName = dr["CustomerName"] != DBNull.Value ? dr["CustomerName"].ToString() : "";
                }
                else
                {
                    TempData["Error"] = "Booking details not found.";
                    return RedirectToAction("MyLabTests");
                }
            }

            return View("~/Views/Customer/LabBookingDetails.cshtml", model);
        }

        [HttpGet]
        public IActionResult CancelLabBooking(long id)
        {
            if (HttpContext.Session.GetString("CustomerId") == null)
            {
                return RedirectToAction("Login");
            }

            long customerId = Convert.ToInt64(HttpContext.Session.GetString("CustomerId"));

            try
            {
                using (SqlConnection con = new SqlConnection(cs))
                {
                    SqlCommand cmd = new SqlCommand(@"
                        UPDATE tbl_LabBooking 
                        SET TestStatus = 'Cancelled', UpdatedDate = GETDATE()
                        WHERE LabBookingId = @LabBookingId AND CustomerId = @CustomerId 
                        AND TestStatus IN ('Pending', 'Sample Collected', 'In Progress')", con);

                    cmd.Parameters.AddWithValue("@LabBookingId", id);
                    cmd.Parameters.AddWithValue("@CustomerId", customerId);

                    con.Open();
                    int rows = cmd.ExecuteNonQuery();

                    if (rows > 0)
                    {
                        TempData["Success"] = "Lab booking cancelled successfully.";
                    }
                    else
                    {
                        TempData["Error"] = "Booking cannot be cancelled in its current state or was not found.";
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction("MyLabTests");
        }

        [HttpGet]
        public IActionResult MyLabReports(string search = "")
        {
            if (HttpContext.Session.GetString("CustomerId") == null)
            {
                return RedirectToAction("Login");
            }

            long customerId = Convert.ToInt64(HttpContext.Session.GetString("CustomerId"));
            List<LabReportModel> list = new List<LabReportModel>();

            using (SqlConnection con = new SqlConnection(cs))
            {
                string query = @"
                    SELECT R.*, B.BookingNo, B.PatientName, B.MobileNo, B.Email, B.BookingDate,
                           T.TestName, T.TestCategory
                    FROM tbl_LabReport R
                    INNER JOIN tbl_LabBooking B ON R.LabBookingId = B.LabBookingId
                    INNER JOIN tbl_LabTest T ON R.LabTestId = T.LabTestId
                    WHERE R.CustomerId = @CustomerId AND R.IsDeleted = 0 AND R.ReportStatus = 'Completed'
                    AND (@Search = '' OR R.ReportNo LIKE '%' + @Search + '%' OR B.BookingNo LIKE '%' + @Search + '%' OR T.TestName LIKE '%' + @Search + '%' OR R.ReportTitle LIKE '%' + @Search + '%')
                    ORDER BY R.LabReportId DESC";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@CustomerId", customerId);
                cmd.Parameters.AddWithValue("@Search", search ?? "");

                con.Open();
                SqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    LabReportModel model = new LabReportModel
                    {
                        LabReportId = Convert.ToInt64(dr["LabReportId"]),
                        LabBookingId = Convert.ToInt64(dr["LabBookingId"]),
                        LabTestId = Convert.ToInt64(dr["LabTestId"]),
                        CustomerId = Convert.ToInt64(dr["CustomerId"]),
                        ReportNo = dr["ReportNo"].ToString(),
                        ReportDate = dr["ReportDate"] != DBNull.Value ? Convert.ToDateTime(dr["ReportDate"]) : null,
                        ReportFile = dr["ReportFile"] != DBNull.Value ? dr["ReportFile"].ToString() : "",
                        ReportTitle = dr["ReportTitle"] != DBNull.Value ? dr["ReportTitle"].ToString() : "",
                        TechnicianName = dr["TechnicianName"] != DBNull.Value ? dr["TechnicianName"].ToString() : "",
                        ReportStatus = dr["ReportStatus"].ToString(),
                        ReportRemark = dr["ReportRemark"] != DBNull.Value ? dr["ReportRemark"].ToString() : "",
                        BookingNo = dr["BookingNo"] != DBNull.Value ? dr["BookingNo"].ToString() : "",
                        PatientName = dr["PatientName"] != DBNull.Value ? dr["PatientName"].ToString() : "",
                        MobileNo = dr["MobileNo"] != DBNull.Value ? dr["MobileNo"].ToString() : "",
                        Email = dr["Email"] != DBNull.Value ? dr["Email"].ToString() : "",
                        TestName = dr["TestName"] != DBNull.Value ? dr["TestName"].ToString() : "",
                        TestCategory = dr["TestCategory"] != DBNull.Value ? dr["TestCategory"].ToString() : ""
                    };
                    list.Add(model);
                }
            }

            ViewBag.Search = search;
            return View("~/Views/Customer/MyLabReports.cshtml", list);
        }

        [HttpGet]
        public IActionResult LabReportDetails(long id)
        {
            if (HttpContext.Session.GetString("CustomerId") == null)
            {
                return RedirectToAction("Login");
            }

            long customerId = Convert.ToInt64(HttpContext.Session.GetString("CustomerId"));
            LabReportModel model = new LabReportModel();

            using (SqlConnection con = new SqlConnection(cs))
            {
                string query = @"
                    SELECT R.*, B.BookingNo, B.PatientName, B.MobileNo, B.Email, B.BookingDate,
                           T.TestName, T.TestCategory
                    FROM tbl_LabReport R
                    INNER JOIN tbl_LabBooking B ON R.LabBookingId = B.LabBookingId
                    INNER JOIN tbl_LabTest T ON R.LabTestId = T.LabTestId
                    WHERE R.LabReportId = @LabReportId AND R.CustomerId = @CustomerId AND R.IsDeleted = 0";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@LabReportId", id);
                cmd.Parameters.AddWithValue("@CustomerId", customerId);

                con.Open();
                SqlDataReader dr = cmd.ExecuteReader();
                if (dr.Read())
                {
                    model.LabReportId = Convert.ToInt64(dr["LabReportId"]);
                    model.LabBookingId = Convert.ToInt64(dr["LabBookingId"]);
                    model.LabTestId = Convert.ToInt64(dr["LabTestId"]);
                    model.CustomerId = Convert.ToInt64(dr["CustomerId"]);
                    model.ReportNo = dr["ReportNo"].ToString();
                    model.ReportDate = dr["ReportDate"] != DBNull.Value ? Convert.ToDateTime(dr["ReportDate"]) : null;
                    model.ReportFile = dr["ReportFile"] != DBNull.Value ? dr["ReportFile"].ToString() : "";
                    model.ReportTitle = dr["ReportTitle"] != DBNull.Value ? dr["ReportTitle"].ToString() : "";
                    model.TechnicianName = dr["TechnicianName"] != DBNull.Value ? dr["TechnicianName"].ToString() : "";
                    model.ReportStatus = dr["ReportStatus"].ToString();
                    model.ReportRemark = dr["ReportRemark"] != DBNull.Value ? dr["ReportRemark"].ToString() : "";
                    model.BookingNo = dr["BookingNo"] != DBNull.Value ? dr["BookingNo"].ToString() : "";
                    model.PatientName = dr["PatientName"] != DBNull.Value ? dr["PatientName"].ToString() : "";
                    model.MobileNo = dr["MobileNo"] != DBNull.Value ? dr["MobileNo"].ToString() : "";
                    model.Email = dr["Email"] != DBNull.Value ? dr["Email"].ToString() : "";
                    model.TestName = dr["TestName"] != DBNull.Value ? dr["TestName"].ToString() : "";
                    model.TestCategory = dr["TestCategory"] != DBNull.Value ? dr["TestCategory"].ToString() : "";
                }
                else
                {
                    TempData["Error"] = "Lab Report details not found.";
                    return RedirectToAction("MyLabReports");
                }
            }

            return View("~/Views/Customer/LabReportDetails.cshtml", model);
        }

        [HttpGet]
        public IActionResult DownloadLabReport(long id)
        {
            if (HttpContext.Session.GetString("CustomerId") == null)
            {
                return RedirectToAction("Login");
            }

            long customerId = Convert.ToInt64(HttpContext.Session.GetString("CustomerId"));
            string reportFile = "";

            using (SqlConnection con = new SqlConnection(cs))
            {
                SqlCommand cmd = new SqlCommand(@"
                    SELECT ReportFile FROM tbl_LabReport 
                    WHERE LabReportId = @LabReportId AND CustomerId = @CustomerId AND IsDeleted = 0", con);

                cmd.Parameters.AddWithValue("@LabReportId", id);
                cmd.Parameters.AddWithValue("@CustomerId", customerId);

                con.Open();
                object obj = cmd.ExecuteScalar();
                if (obj != null)
                {
                    reportFile = obj.ToString();
                }
            }

            if (string.IsNullOrEmpty(reportFile))
            {
                TempData["Error"] = "Lab report file not found or permission denied.";
                return RedirectToAction("MyLabReports");
            }

            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "LabReports", reportFile);
            if (!System.IO.File.Exists(filePath))
            {
                TempData["Error"] = "Report PDF file does not exist on server.";
                return RedirectToAction("MyLabReports");
            }

            byte[] fileBytes = System.IO.File.ReadAllBytes(filePath);
            return File(fileBytes, "application/pdf", reportFile);
        }
        [HttpGet]
        public IActionResult BookHealthPackage(long id)
        {
            if (HttpContext.Session.GetString("CustomerId") == null)
            {
                TempData["Error"] = "Please sign in to book a health package.";
                return RedirectToAction("Login");
            }

            HealthPackageBookingModel model =
                new HealthPackageBookingModel();

            try
            {
                long customerId =
                    Convert.ToInt64(
                        HttpContext.Session.GetString("CustomerId"));

                model.CustomerId = customerId;
                model.HealthPackageId = id;

                using (SqlConnection con = new SqlConnection(cs))
                {
                    con.Open();


                    string packageQuery = @"
                SELECT
                    HealthPackageId,
                    PackageName,
                    DiscountPrice
                FROM tbl_HealthPackage
                WHERE HealthPackageId = @HealthPackageId
                AND IsDeleted = 0
                AND IsActive = 1";

                    using (SqlCommand cmd =
                           new SqlCommand(packageQuery, con))
                    {
                        cmd.Parameters.AddWithValue(
                            "@HealthPackageId", id);

                        using (SqlDataReader dr =
                               cmd.ExecuteReader())
                        {
                            if (!dr.Read())
                            {
                                TempData["Error"] =
                                    "Selected health package is not available.";

                                return RedirectToAction(
                                    "HealthPackages");
                            }

                            model.HealthPackageId =
                                Convert.ToInt64(
                                    dr["HealthPackageId"]);

                            model.PackageName =
                                dr["PackageName"] == DBNull.Value
                                ? ""
                                : dr["PackageName"].ToString();

                            model.Amount =
                                dr["DiscountPrice"] == DBNull.Value
                                ? 0
                                : Convert.ToDecimal(
                                    dr["DiscountPrice"]);
                        }
                    }

                    string customerQuery = @"
                SELECT
                    FullName,
                    MobileNo,
                    Email
                FROM tbl_Customer
                WHERE CustomerId = @CustomerId
                AND IsDeleted = 0";

                    using (SqlCommand cmd =
                           new SqlCommand(customerQuery, con))
                    {
                        cmd.Parameters.AddWithValue(
                            "@CustomerId", customerId);

                        using (SqlDataReader dr =
                               cmd.ExecuteReader())
                        {
                            if (dr.Read())
                            {
                                model.PatientName =
                                    dr["FullName"] == DBNull.Value
                                    ? ""
                                    : dr["FullName"].ToString();

                                model.MobileNo =
                                    dr["MobileNo"] == DBNull.Value
                                    ? ""
                                    : dr["MobileNo"].ToString();

                                model.Email =
                                    dr["Email"] == DBNull.Value
                                    ? ""
                                    : dr["Email"].ToString();
                            }
                        }
                    }
                }

                model.PreferredDate =
                    DateTime.Today.AddDays(1);

                model.PreferredTime = "";

                return View(
                    "~/Views/Customer/BookHealthPackage.cshtml",
                    model);
            }
            catch (Exception ex)
            {
                TempData["Error"] =
                    "Unable to load health package: " + ex.Message;

                return RedirectToAction(
                    "HealthPackages");
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult BookHealthPackage(
    HealthPackageBookingModel model)
        {
            if (HttpContext.Session.GetString("CustomerId") == null)
            {
                TempData["Error"] =
                    "Please sign in to book a health package.";

                return RedirectToAction("Login");
            }

            try
            {
                long customerId =
                    Convert.ToInt64(
                        HttpContext.Session.GetString("CustomerId"));

                model.CustomerId = customerId;

                string bookingNo =
                    "HPB" + DateTime.Now.ToString("yyyyMMddHHmmssfff");

                using (SqlConnection con =
                       new SqlConnection(cs))
                {
                    con.Open();


                    decimal amount = 0;

                    string packageQuery = @"
                SELECT
                    PackageName,
                    DiscountPrice
                FROM tbl_HealthPackage
                WHERE HealthPackageId = @HealthPackageId
                AND IsDeleted = 0
                AND IsActive = 1";

                    using (SqlCommand packageCmd =
                           new SqlCommand(packageQuery, con))
                    {
                        packageCmd.Parameters.AddWithValue(
                            "@HealthPackageId",
                            model.HealthPackageId);

                        using (SqlDataReader dr =
                               packageCmd.ExecuteReader())
                        {
                            if (!dr.Read())
                            {
                                TempData["Error"] =
                                    "Selected health package is not available.";

                                return RedirectToAction(
                                    "HealthPackages");
                            }

                            amount =
                                dr["DiscountPrice"] == DBNull.Value
                                ? 0
                                : Convert.ToDecimal(
                                    dr["DiscountPrice"]);

                            model.PackageName =
                                dr["PackageName"] == DBNull.Value
                                ? ""
                                : dr["PackageName"].ToString();
                        }
                    }

                    string insertQuery = @"
                INSERT INTO tbl_HealthPackageBooking
                (
                    HealthPackageId,
                    CustomerId,
                    BookingNo,
                    BookingDate,
                    PatientName,
                    MobileNo,
                    Email,
                    PreferredDate,
                    PreferredTime,
                    HealthConcern,
                    Amount,
                    PaymentStatus,
                    BookingStatus,
                    AdminRemark,
                    IsActive,
                    IsDeleted,
                    CreatedDate
                )
                VALUES
                (
                    @HealthPackageId,
                    @CustomerId,
                    @BookingNo,
                    GETDATE(),
                    @PatientName,
                    @MobileNo,
                    @Email,
                    @PreferredDate,
                    @PreferredTime,
                    @HealthConcern,
                    @Amount,
                    'Pending',
                    'Pending',
                    NULL,
                    1,
                    0,
                    GETDATE()
                )";

                    using (SqlCommand cmd =
                           new SqlCommand(insertQuery, con))
                    {
                        cmd.Parameters.AddWithValue(
                            "@HealthPackageId",
                            model.HealthPackageId);

                        cmd.Parameters.AddWithValue(
                            "@CustomerId",
                            model.CustomerId);

                        cmd.Parameters.AddWithValue(
                            "@BookingNo",
                            bookingNo);

                        cmd.Parameters.AddWithValue(
                            "@PatientName",
                            model.PatientName ?? "");

                        cmd.Parameters.AddWithValue(
                            "@MobileNo",
                            model.MobileNo ?? "");

                        cmd.Parameters.AddWithValue(
                            "@Email",
                            string.IsNullOrWhiteSpace(model.Email)
                            ? (object)DBNull.Value
                            : model.Email);

                        cmd.Parameters.AddWithValue(
                            "@PreferredDate",
                            model.PreferredDate.HasValue
                            ? (object)model.PreferredDate.Value
                            : DBNull.Value);

                        cmd.Parameters.AddWithValue(
                            "@PreferredTime",
                            string.IsNullOrWhiteSpace(model.PreferredTime)
                            ? (object)DBNull.Value
                            : model.PreferredTime);

                        cmd.Parameters.AddWithValue(
                            "@HealthConcern",
                            string.IsNullOrWhiteSpace(model.HealthConcern)
                            ? (object)DBNull.Value
                            : model.HealthConcern);

                        cmd.Parameters.AddWithValue(
                            "@Amount",
                            amount);

                        int rows =
                            cmd.ExecuteNonQuery();

                        if (rows <= 0)
                        {
                            TempData["Error"] =
                                "Booking could not be saved.";

                            return RedirectToAction(
                                "HealthPackages");
                        }
                    }
                }

                TempData["Success"] =
                    "Health package booked successfully! Booking No: "
                    + bookingNo;

                return RedirectToAction(
                    "MyHealthPackageBookings");
            }
            catch (Exception ex)
            {
                TempData["Error"] =
                    "Unable to book health package: "
                    + ex.Message;

                return RedirectToAction(
                    "BookHealthPackage",
                    new
                    {
                        id = model.HealthPackageId
                    });
            }
        }
        [HttpGet]
        public IActionResult MyHealthPackageBookings()
        {
            if (HttpContext.Session.GetString("CustomerId") == null)
            {
                TempData["Error"] =
                    "Please sign in to view your bookings.";

                return RedirectToAction("Login");
            }

            List<HealthPackageBookingModel> list =
                new List<HealthPackageBookingModel>();

            try
            {
                long customerId =
                    Convert.ToInt64(
                        HttpContext.Session.GetString("CustomerId"));

                using (SqlConnection con =
                       new SqlConnection(cs))
                {
                    con.Open();

                    string query = @"
                SELECT
                    pb.PackageBookingId,
                    pb.HealthPackageId,
                    pb.CustomerId,
                    pb.BookingNo,
                    pb.BookingDate,
                    pb.PatientName,
                    pb.MobileNo,
                    pb.Email,
                    pb.PreferredDate,
                    pb.PreferredTime,
                    pb.HealthConcern,
                    pb.Amount,
                    pb.PaymentStatus,
                    pb.BookingStatus,
                    pb.AdminRemark,
                    pb.IsActive,
                    pb.IsDeleted,
                    pb.CreatedDate,
                    pb.UpdatedDate,

                    hp.PackageName

                FROM tbl_HealthPackageBooking pb

                INNER JOIN tbl_HealthPackage hp
                    ON pb.HealthPackageId =
                       hp.HealthPackageId

                WHERE
                    pb.CustomerId = @CustomerId
                    AND pb.IsDeleted = 0

                ORDER BY
                    pb.BookingDate DESC";

                    using (SqlCommand cmd =
                           new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue(
                            "@CustomerId", customerId);

                        using (SqlDataReader dr =
                               cmd.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                HealthPackageBookingModel model =
                                    new HealthPackageBookingModel();

                                model.PackageBookingId =
                                    Convert.ToInt64(
                                        dr["PackageBookingId"]);

                                model.HealthPackageId =
                                    Convert.ToInt64(
                                        dr["HealthPackageId"]);

                                model.CustomerId =
                                    Convert.ToInt64(
                                        dr["CustomerId"]);

                                model.BookingNo =
                                    dr["BookingNo"] == DBNull.Value
                                    ? ""
                                    : dr["BookingNo"].ToString();

                                if (dr["BookingDate"] != DBNull.Value)
                                {
                                    model.BookingDate =
                                        Convert.ToDateTime(
                                            dr["BookingDate"]);
                                }

                                model.PatientName =
                                    dr["PatientName"] == DBNull.Value
                                    ? ""
                                    : dr["PatientName"].ToString();

                                model.MobileNo =
                                    dr["MobileNo"] == DBNull.Value
                                    ? ""
                                    : dr["MobileNo"].ToString();

                                model.Email =
                                    dr["Email"] == DBNull.Value
                                    ? ""
                                    : dr["Email"].ToString();

                                if (dr["PreferredDate"] != DBNull.Value)
                                {
                                    model.PreferredDate =
                                        Convert.ToDateTime(
                                            dr["PreferredDate"]);
                                }

                                model.PreferredTime =
                                    dr["PreferredTime"] == DBNull.Value
                                    ? ""
                                    : dr["PreferredTime"].ToString();

                                model.HealthConcern =
                                    dr["HealthConcern"] == DBNull.Value
                                    ? ""
                                    : dr["HealthConcern"].ToString();

                                model.Amount =
                                    dr["Amount"] == DBNull.Value
                                    ? 0
                                    : Convert.ToDecimal(
                                        dr["Amount"]);

                                model.PaymentStatus =
                                    dr["PaymentStatus"] == DBNull.Value
                                    ? ""
                                    : dr["PaymentStatus"].ToString();

                                model.BookingStatus =
                                    dr["BookingStatus"] == DBNull.Value
                                    ? ""
                                    : dr["BookingStatus"].ToString();

                                model.AdminRemark =
                                    dr["AdminRemark"] == DBNull.Value
                                    ? ""
                                    : dr["AdminRemark"].ToString();

                                model.PackageName =
                                    dr["PackageName"] == DBNull.Value
                                    ? ""
                                    : dr["PackageName"].ToString();

                                list.Add(model);
                            }
                        }
                    }
                }

                return View(
                    "~/Views/Customer/MyHealthPackageBookings.cshtml",
                    list);
            }
            catch (Exception ex)
            {
                TempData["Error"] =
                    "Unable to load your bookings: " + ex.Message;

                return View(
                    "~/Views/Customer/MyHealthPackageBookings.cshtml",
                    list);
            }
        }
        [HttpGet]
        public IActionResult HealthPackageBookingDetails(long id)
        {
            if (HttpContext.Session.GetString("CustomerId") == null)
            {
                TempData["Error"] =
                    "Please sign in to view booking details.";

                return RedirectToAction("Login");
            }

            HealthPackageBookingModel model =
                new HealthPackageBookingModel();

            try
            {
                long customerId =
                    Convert.ToInt64(
                        HttpContext.Session.GetString("CustomerId"));

                using (SqlConnection con =
                       new SqlConnection(cs))
                {
                    con.Open();

                    string query = @"
                SELECT
                    pb.PackageBookingId,
                    pb.HealthPackageId,
                    pb.CustomerId,
                    pb.BookingNo,
                    pb.BookingDate,
                    pb.PatientName,
                    pb.MobileNo,
                    pb.Email,
                    pb.PreferredDate,
                    pb.PreferredTime,
                    pb.HealthConcern,
                    pb.Amount,
                    pb.PaymentStatus,
                    pb.BookingStatus,
                    pb.AdminRemark,
                    pb.IsActive,
                    pb.IsDeleted,
                    pb.CreatedDate,
                    pb.UpdatedDate,
                    hp.PackageName
                FROM tbl_HealthPackageBooking pb
                INNER JOIN tbl_HealthPackage hp
                    ON pb.HealthPackageId =
                       hp.HealthPackageId
                WHERE
                    pb.PackageBookingId =
                        @PackageBookingId
                    AND pb.CustomerId = @CustomerId
                    AND pb.IsDeleted = 0";

                    using (SqlCommand cmd =
                           new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue(
                            "@PackageBookingId", id);

                        cmd.Parameters.AddWithValue(
                            "@CustomerId", customerId);

                        using (SqlDataReader dr =
                               cmd.ExecuteReader())
                        {
                            if (!dr.Read())
                            {
                                TempData["Error"] =
                                    "Booking not found.";

                                return RedirectToAction(
                                    "MyHealthPackageBookings");
                            }

                            model.PackageBookingId =
                                Convert.ToInt64(
                                    dr["PackageBookingId"]);

                            model.HealthPackageId =
                                Convert.ToInt64(
                                    dr["HealthPackageId"]);

                            model.CustomerId =
                                Convert.ToInt64(
                                    dr["CustomerId"]);

                            model.BookingNo =
                                dr["BookingNo"] == DBNull.Value
                                ? ""
                                : dr["BookingNo"].ToString();

                            if (dr["BookingDate"] != DBNull.Value)
                            {
                                model.BookingDate =
                                    Convert.ToDateTime(
                                        dr["BookingDate"]);
                            }

                            model.PatientName =
                                dr["PatientName"] == DBNull.Value
                                ? ""
                                : dr["PatientName"].ToString();

                            model.MobileNo =
                                dr["MobileNo"] == DBNull.Value
                                ? ""
                                : dr["MobileNo"].ToString();

                            model.Email =
                                dr["Email"] == DBNull.Value
                                ? ""
                                : dr["Email"].ToString();

                            if (dr["PreferredDate"] != DBNull.Value)
                            {
                                model.PreferredDate =
                                    Convert.ToDateTime(
                                        dr["PreferredDate"]);
                            }

                            model.PreferredTime =
                                dr["PreferredTime"] == DBNull.Value
                                ? ""
                                : dr["PreferredTime"].ToString();

                            model.HealthConcern =
                                dr["HealthConcern"] == DBNull.Value
                                ? ""
                                : dr["HealthConcern"].ToString();

                            model.Amount =
                                dr["Amount"] == DBNull.Value
                                ? 0
                                : Convert.ToDecimal(
                                    dr["Amount"]);

                            model.PaymentStatus =
                                dr["PaymentStatus"] == DBNull.Value
                                ? ""
                                : dr["PaymentStatus"].ToString();

                            model.BookingStatus =
                                dr["BookingStatus"] == DBNull.Value
                                ? ""
                                : dr["BookingStatus"].ToString();

                            model.AdminRemark =
                                dr["AdminRemark"] == DBNull.Value
                                ? ""
                                : dr["AdminRemark"].ToString();

                            model.PackageName =
                                dr["PackageName"] == DBNull.Value
                                ? ""
                                : dr["PackageName"].ToString();

                            if (dr["CreatedDate"] != DBNull.Value)
                            {
                                model.CreatedDate =
                                    Convert.ToDateTime(
                                        dr["CreatedDate"]);
                            }

                            if (dr["UpdatedDate"] != DBNull.Value)
                            {
                                model.UpdatedDate =
                                    Convert.ToDateTime(
                                        dr["UpdatedDate"]);
                            }
                        }
                    }
                }

                return View(
                    "~/Views/Customer/HealthPackageBookingDetails.cshtml",
                    model);
            }
            catch (Exception ex)
            {
                TempData["Error"] =
                    "Unable to load booking: " + ex.Message;

                return RedirectToAction(
                    "MyHealthPackageBookings");
            }
        }
        [HttpGet]
        public IActionResult CancelHealthPackageBooking(long id)
        {
            if (HttpContext.Session.GetString("CustomerId") == null)
            {
                TempData["Error"] =
                    "Please sign in to manage your booking.";

                return RedirectToAction("Login");
            }

            try
            {
                long customerId =
                    Convert.ToInt64(
                        HttpContext.Session.GetString("CustomerId"));

                using (SqlConnection con =
                       new SqlConnection(cs))
                {
                    con.Open();

                    string query = @"
                UPDATE tbl_HealthPackageBooking
                SET
                    BookingStatus = 'Cancelled',
                    IsActive = 0,
                    UpdatedDate = GETDATE()
                WHERE
                    PackageBookingId = @PackageBookingId
                    AND CustomerId = @CustomerId
                    AND IsDeleted = 0
                    AND BookingStatus = 'Pending'";

                    using (SqlCommand cmd =
                           new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue(
                            "@PackageBookingId", id);

                        cmd.Parameters.AddWithValue(
                            "@CustomerId", customerId);

                        int rows =
                            cmd.ExecuteNonQuery();

                        if (rows > 0)
                        {
                            TempData["Success"] =
                                "Health package booking cancelled successfully.";
                        }
                        else
                        {
                            TempData["Error"] =
                                "Booking cannot be cancelled.";
                        }
                    }
                }

                return RedirectToAction(
                    "MyHealthPackageBookings");
            }
            catch (Exception ex)
            {
                TempData["Error"] =
                    "Unable to cancel booking: " + ex.Message;

                return RedirectToAction(
                    "MyHealthPackageBookings");
            }
        }
        [HttpGet]
        public IActionResult HealthPackages()
        {
            if (HttpContext.Session.GetString("CustomerId") == null)
            {
                TempData["Error"] = "Please sign in to view health packages.";
                return RedirectToAction("Login");
            }

            List<HealthPackageModel> list =
                new List<HealthPackageModel>();

            try
            {
                using (SqlConnection con = new SqlConnection(cs))
                {
                    con.Open();

                    string query = @"
                SELECT
                    HealthPackageId,
                    PackageName,
                    DiscountPrice,
                    IsActive,
                    IsDeleted
                FROM tbl_HealthPackage
                WHERE IsDeleted = 0
                AND IsActive = 1
                ORDER BY HealthPackageId DESC";

                    using (SqlCommand cmd =
                           new SqlCommand(query, con))
                    {
                        using (SqlDataReader dr =
                               cmd.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                HealthPackageModel model =
                                    new HealthPackageModel();

                                model.HealthPackageId =
                                    Convert.ToInt64(
                                        dr["HealthPackageId"]);

                                model.PackageName =
                                    dr["PackageName"] == DBNull.Value
                                    ? ""
                                    : dr["PackageName"].ToString();

                                model.DiscountPrice =
                                    dr["DiscountPrice"] == DBNull.Value
                                    ? 0
                                    : Convert.ToDecimal(
                                        dr["DiscountPrice"]);

                                model.IsActive =
                                    dr["IsActive"] != DBNull.Value &&
                                    Convert.ToBoolean(
                                        dr["IsActive"]);

                                model.IsDeleted =
                                    dr["IsDeleted"] != DBNull.Value &&
                                    Convert.ToBoolean(
                                        dr["IsDeleted"]);

                                list.Add(model);
                            }
                        }
                    }
                }

                return View(
                    "~/Views/Customer/HealthPackages.cshtml",
                    list);
            }
            catch (Exception ex)
            {
                TempData["Error"] =
                    "Unable to load health packages: " + ex.Message;

                return View(
                    "~/Views/Customer/HealthPackages.cshtml",
                    list);
            }
        }
        [HttpGet]
        public IActionResult HealthPackageDetails(long id)
        {
            if (HttpContext.Session.GetString("CustomerId") == null)
            {
                TempData["Error"] = "Please sign in to view health package details.";
                return RedirectToAction("Login");
            }

            HealthPackageModel model = new HealthPackageModel();

            try
            {
                using (SqlConnection con = new SqlConnection(cs))
                {
                    con.Open();

                    string query = @"
                SELECT
                    HealthPackageId,
                    PackageName,
                    DiscountPrice,
                    IsActive,
                    IsDeleted
                FROM tbl_HealthPackage
                WHERE HealthPackageId = @HealthPackageId
                AND IsDeleted = 0
                AND IsActive = 1";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@HealthPackageId", id);

                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            if (!dr.Read())
                            {
                                TempData["Error"] =
                                    "Selected health package is not available.";

                                return RedirectToAction("HealthPackages");
                            }

                            model.HealthPackageId =
                                Convert.ToInt64(dr["HealthPackageId"]);

                            model.PackageName =
                                dr["PackageName"] == DBNull.Value
                                ? ""
                                : dr["PackageName"].ToString();

                            model.DiscountPrice =
                                dr["DiscountPrice"] == DBNull.Value
                                ? 0
                                : Convert.ToDecimal(dr["DiscountPrice"]);

                            model.IsActive =
                                dr["IsActive"] != DBNull.Value &&
                                Convert.ToBoolean(dr["IsActive"]);

                            model.IsDeleted =
                                dr["IsDeleted"] != DBNull.Value &&
                                Convert.ToBoolean(dr["IsDeleted"]);
                        }
                    }
                }

                return View(
                    "~/Views/Customer/HealthPackageDetails.cshtml",
                    model);
            }
            catch (Exception ex)
            {
                TempData["Error"] =
                    "Unable to load health package details: " + ex.Message;

                return RedirectToAction("HealthPackages");
            }
        }
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}

