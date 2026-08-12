using ClinicManagementSystem.Models;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Data.SqlClient;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

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
                                        .Text("• Take medicines exactly as prescribed by your doctor.");

                                    note.Item()
                                        .Text("• Do not skip doses or stop medicines without consulting the doctor.");

                                    note.Item()
                                        .Text("• Drink plenty of water and maintain a healthy diet.");

                                    note.Item()
                                        .Text("• Visit the clinic immediately if symptoms worsen.");

                                    note.Item()
                                        .Text("• Carry this prescription during your next visit.");
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
                                        .Text("• This invoice is generated by Clinic Management System.");

                                    note.Item()
                                        .Text("• Please keep this bill safely for future medical reference.");

                                    note.Item()
                                        .Text("• Contact the clinic if you have any billing questions.");

                                    note.Item()
                                        .Text("• We appreciate your trust in our healthcare services.");
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
  
       
      
        // ==========================================
        // GET: /Customer/Logout
        // ==========================================
        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}

