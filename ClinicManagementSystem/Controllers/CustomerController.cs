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
        SELECT d.*, dep.DepartmentName
        FROM tbl_Doctor d
        INNER JOIN tbl_Department dep
            ON d.DepartmentId = dep.DepartmentId
        WHERE d.IsDeleted = 0
        AND d.IsActive = 1";

                if (!string.IsNullOrEmpty(search))
                {
                    query += @" AND
                        (
                            d.DoctorName LIKE @Search
                            OR d.Specialization LIKE @Search
                        )";
                }

                if (departmentId > 0)
                {
                    query += " AND d.DepartmentId = @DepartmentId";
                }

                query += " ORDER BY d.DoctorName ASC";

                SqlCommand cmd = new SqlCommand(query, con);

                if (!string.IsNullOrEmpty(search))
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
                    model.AvailableFrom = (TimeSpan?)dr["AvailableFrom"];
                    model.AvailableTo = (TimeSpan?)dr["AvailableTo"];
                    model.Description = dr["Description"].ToString();

                    list.Add(model);
                }

                dr.Close();

                
                SqlCommand deptCmd = new SqlCommand(@"
            SELECT DepartmentId, DepartmentName
            FROM tbl_Department
            WHERE IsActive = 1
            AND IsDeleted = 0
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
        SELECT d.*, dep.DepartmentName
        FROM tbl_Doctor d
        INNER JOIN tbl_Department dep
            ON d.DepartmentId = dep.DepartmentId
        WHERE d.DoctorId = @DoctorId
        AND d.IsDeleted = 0
        AND d.IsActive = 1";

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

                    SqlCommand cmd = new SqlCommand(@"
            SELECT d.DoctorName,
                   d.DoctorImage,
                   dep.DepartmentName,
                   d.ConsultationFee,
                   d.AvailableFrom,
                   d.AvailableTo
            FROM tbl_Doctor d
            INNER JOIN tbl_Department dep
            ON d.DepartmentId = dep.DepartmentId
            WHERE d.DoctorId=@DoctorId", con);

                    cmd.Parameters.AddWithValue("@DoctorId", model.DoctorId);

                    SqlDataReader dr = cmd.ExecuteReader();

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

            try
            {
                using (SqlConnection con = new SqlConnection(cs))
                {
                    con.Open();

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
                    else
                    {
                        TempData["Error"] = "Appointment booking failed.";
                        return View(model);
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;

                
                using (SqlConnection con = new SqlConnection(cs))
                {
                    con.Open();

                    SqlCommand cmd = new SqlCommand(@"
            SELECT d.DoctorName,
                   d.DoctorImage,
                   dep.DepartmentName,
                   d.ConsultationFee,
                   d.AvailableFrom,
                   d.AvailableTo
            FROM tbl_Doctor d
            INNER JOIN tbl_Department dep
            ON d.DepartmentId = dep.DepartmentId
            WHERE d.DoctorId=@DoctorId", con);

                    cmd.Parameters.AddWithValue("@DoctorId", model.DoctorId);

                    SqlDataReader dr = cmd.ExecuteReader();

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

