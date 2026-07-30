using ClinicManagementSystem.Models;
using Microsoft.AspNetCore.Mvc;
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

