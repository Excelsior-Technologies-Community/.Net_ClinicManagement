using ClinicManagementSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Data;
using System.Data.SqlClient;

namespace ClinicManagementSystem.Controllers
{
    public class AdminController : Controller
    {
        string cs = @"Data Source=DESKTOP-J2OEC9R\SQLEXPRESS02;Initial Catalog=ClinicDB;Integrated Security=True;TrustServerCertificate=True";

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (HttpContext.Session.GetString("AdminId") != null)
            {
                return RedirectToAction("Dashboard");
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(AdminModel model)
        {
            // Login માટે જરૂરી ન હોય તેવા fields ની validation remove કરો
            ModelState.Remove("FullName");
            ModelState.Remove("ConfirmPassword");
            ModelState.Remove("MobileNo");
            ModelState.Remove("ImageFile");
            ModelState.Remove("ProfileImage");

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

                SqlCommand cmd = new SqlCommand(@"
            SELECT AdminId, FullName, Email
            FROM tbl_Admin
            WHERE Email = @Email
            AND Password = @Password
            AND IsActive = 1
            AND IsDeleted = 0", con);

                cmd.Parameters.AddWithValue("@Email", model.Email!);
                cmd.Parameters.AddWithValue("@Password", model.Password!);

                SqlDataReader dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    HttpContext.Session.SetString("AdminId", dr["AdminId"].ToString()!);
                    HttpContext.Session.SetString("FullName", dr["FullName"].ToString()!);
                    HttpContext.Session.SetString("Email", dr["Email"].ToString()!);

                    return RedirectToAction("Dashboard");
                }
            }

            ViewBag.Error = "Invalid Email or Password";
            return View(model);
        }

        [HttpGet]
        public IActionResult Dashboard()
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            return View();
        }
        [HttpGet]
        public IActionResult ManageDepartment(string search = "")
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            List<DepartmentModel> list = new List<DepartmentModel>();

            using (SqlConnection con = new SqlConnection(cs))
            {
                string query = @"SELECT *
                         FROM tbl_Department
                         WHERE IsDeleted = 0
                         AND DepartmentName LIKE @Search
                         ORDER BY DisplayOrder ASC";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@Search", "%" + search + "%");

                con.Open();

                SqlDataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    DepartmentModel model = new DepartmentModel();

                    model.DepartmentId = Convert.ToInt64(dr["DepartmentId"]);
                    model.DepartmentName = dr["DepartmentName"].ToString();
                    model.DepartmentDescription = dr["DepartmentDescription"].ToString();
                    model.DepartmentImage = dr["DepartmentImage"].ToString();
                    model.DisplayOrder = Convert.ToInt32(dr["DisplayOrder"]);
                    model.IsActive = Convert.ToBoolean(dr["IsActive"]);
                    model.IsDeleted = Convert.ToBoolean(dr["IsDeleted"]);
                    model.CreatedDate = Convert.ToDateTime(dr["CreatedDate"]);

                    if (dr["UpdatedDate"] != DBNull.Value)
                    {
                        model.UpdatedDate = Convert.ToDateTime(dr["UpdatedDate"]);
                    }

                    list.Add(model);
                }

                con.Close();
            }

            ViewBag.Search = search;

            return View(list);
        }
      
        [HttpGet]
        public IActionResult AddDepartment()
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            return View();
        }
       
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddDepartment(DepartmentModel model)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            ModelState.Remove("DepartmentImage");

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                using (SqlConnection con = new SqlConnection(cs))
                {
                    con.Open();

                    // Duplicate Department Check
                    SqlCommand checkCmd = new SqlCommand(
                        "SELECT COUNT(*) FROM tbl_Department WHERE DepartmentName=@DepartmentName AND IsDeleted=0",
                        con);

                    checkCmd.Parameters.AddWithValue("@DepartmentName", model.DepartmentName);

                    int count = Convert.ToInt32(checkCmd.ExecuteScalar());

                    if (count > 0)
                    {
                        ViewBag.Error = "Department already exists.";
                        return View(model);
                    }

                    // Image Upload
                    string fileName = "";

                    if (model.ImageFile != null)
                    {
                        string folderPath = Path.Combine(Directory.GetCurrentDirectory(),
                                                         "wwwroot",
                                                         "Department");

                        if (!Directory.Exists(folderPath))
                        {
                            Directory.CreateDirectory(folderPath);
                        }

                        fileName = Guid.NewGuid().ToString() +
                                   Path.GetExtension(model.ImageFile.FileName);

                        string filePath = Path.Combine(folderPath, fileName);

                        using (FileStream stream = new FileStream(filePath, FileMode.Create))
                        {
                            model.ImageFile.CopyTo(stream);
                        }
                    }

                    // Insert Department
                    SqlCommand cmd = new SqlCommand(@"
            INSERT INTO tbl_Department
            (
                DepartmentName,
                DepartmentDescription,
                DepartmentImage,
                DisplayOrder,
                IsActive,
                IsDeleted,
                CreatedDate
            )
            VALUES
            (
                @DepartmentName,
                @DepartmentDescription,
                @DepartmentImage,
                @DisplayOrder,
                1,
                0,
                GETDATE()
            )", con);

                    cmd.Parameters.AddWithValue("@DepartmentName", model.DepartmentName);
                    cmd.Parameters.AddWithValue("@DepartmentDescription", model.DepartmentDescription);
                    cmd.Parameters.AddWithValue("@DepartmentImage", fileName);
                    cmd.Parameters.AddWithValue("@DisplayOrder", model.DisplayOrder);

                    cmd.ExecuteNonQuery();

                    TempData["Success"] = "Department added successfully.";

                    return RedirectToAction("ManageDepartment");
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                return View(model);
            }
        }
        [HttpGet]
        public IActionResult EditDepartment(long id)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            DepartmentModel model = new DepartmentModel();

            using (SqlConnection con = new SqlConnection(cs))
            {
                SqlCommand cmd = new SqlCommand("SELECT * FROM tbl_Department WHERE DepartmentId=@DepartmentId AND IsDeleted=0", con);

                cmd.Parameters.AddWithValue("@DepartmentId", id);

                con.Open();

                SqlDataReader dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    model.DepartmentId = Convert.ToInt64(dr["DepartmentId"]);
                    model.DepartmentName = dr["DepartmentName"].ToString();
                    model.DepartmentDescription = dr["DepartmentDescription"].ToString();
                    model.DepartmentImage = dr["DepartmentImage"].ToString();
                    model.DisplayOrder = Convert.ToInt32(dr["DisplayOrder"]);
                    model.IsActive = Convert.ToBoolean(dr["IsActive"]);
                }

                con.Close();
            }

            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditDepartment(DepartmentModel model)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            ModelState.Remove("DepartmentImage");

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                using (SqlConnection con = new SqlConnection(cs))
                {
                    con.Open();

                    // Duplicate Department Name Check
                    SqlCommand checkCmd = new SqlCommand(
                        @"SELECT COUNT(*) FROM tbl_Department
                  WHERE DepartmentName=@DepartmentName
                  AND DepartmentId<>@DepartmentId
                  AND IsDeleted=0", con);

                    checkCmd.Parameters.AddWithValue("@DepartmentName", model.DepartmentName);
                    checkCmd.Parameters.AddWithValue("@DepartmentId", model.DepartmentId);

                    int count = Convert.ToInt32(checkCmd.ExecuteScalar());

                    if (count > 0)
                    {
                        ViewBag.Error = "Department already exists.";
                        return View(model);
                    }

                    // Get Old Image
                    string oldImage = "";

                    SqlCommand imgCmd = new SqlCommand(
                        "SELECT DepartmentImage FROM tbl_Department WHERE DepartmentId=@DepartmentId", con);

                    imgCmd.Parameters.AddWithValue("@DepartmentId", model.DepartmentId);

                    object obj = imgCmd.ExecuteScalar();

                    if (obj != null)
                    {
                        oldImage = obj.ToString();
                    }

                    string fileName = oldImage;

                    // Upload New Image
                    if (model.ImageFile != null)
                    {
                        string folderPath = Path.Combine(
                            Directory.GetCurrentDirectory(),
                            "wwwroot",
                            "Department");

                        if (!Directory.Exists(folderPath))
                        {
                            Directory.CreateDirectory(folderPath);
                        }

                        // Delete Old Image
                        if (!string.IsNullOrEmpty(oldImage))
                        {
                            string oldPath = Path.Combine(folderPath, oldImage);

                            if (System.IO.File.Exists(oldPath))
                            {
                                System.IO.File.Delete(oldPath);
                            }
                        }

                        fileName = Guid.NewGuid().ToString() +
                                   Path.GetExtension(model.ImageFile.FileName);

                        string newPath = Path.Combine(folderPath, fileName);

                        using (FileStream stream = new FileStream(newPath, FileMode.Create))
                        {
                            model.ImageFile.CopyTo(stream);
                        }
                    }

                    // Update Record
                    SqlCommand cmd = new SqlCommand(@"
                UPDATE tbl_Department
                SET DepartmentName=@DepartmentName,
                    DepartmentDescription=@DepartmentDescription,
                    DepartmentImage=@DepartmentImage,
                    DisplayOrder=@DisplayOrder,
                    UpdatedDate=GETDATE()
                WHERE DepartmentId=@DepartmentId", con);

                    cmd.Parameters.AddWithValue("@DepartmentName", model.DepartmentName);
                    cmd.Parameters.AddWithValue("@DepartmentDescription", model.DepartmentDescription);
                    cmd.Parameters.AddWithValue("@DepartmentImage", fileName);
                    cmd.Parameters.AddWithValue("@DisplayOrder", model.DisplayOrder);
                    cmd.Parameters.AddWithValue("@DepartmentId", model.DepartmentId);

                    cmd.ExecuteNonQuery();

                    TempData["Success"] = "Department updated successfully.";

                    return RedirectToAction("ManageDepartment");
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                return View(model);
            }
        }
      
        [HttpGet]
        public IActionResult ViewDepartment(long id)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            DepartmentModel model = new DepartmentModel();

            using (SqlConnection con = new SqlConnection(cs))
            {
                SqlCommand cmd = new SqlCommand(@"
            SELECT *
            FROM tbl_Department
            WHERE DepartmentId = @DepartmentId
            AND IsDeleted = 0", con);

                cmd.Parameters.AddWithValue("@DepartmentId", id);

                con.Open();

                SqlDataReader dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    model.DepartmentId = Convert.ToInt64(dr["DepartmentId"]);
                    model.DepartmentName = dr["DepartmentName"].ToString();
                    model.DepartmentDescription = dr["DepartmentDescription"].ToString();
                    model.DepartmentImage = dr["DepartmentImage"].ToString();
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
                    TempData["Error"] = "Department not found.";
                    return RedirectToAction("ManageDepartment");
                }

                con.Close();
            }

            return View(model);
        }
    
        [HttpGet]
        public IActionResult DeleteDepartment(long id)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            try
            {
                using (SqlConnection con = new SqlConnection(cs))
                {
                    SqlCommand cmd = new SqlCommand(@"
                UPDATE tbl_Department
                SET IsDeleted = 1,
                    UpdatedDate = GETDATE()
                WHERE DepartmentId = @DepartmentId", con);

                    cmd.Parameters.AddWithValue("@DepartmentId", id);

                    con.Open();

                    int result = cmd.ExecuteNonQuery();

                    con.Close();

                    if (result > 0)
                    {
                        TempData["Success"] = "Department deleted successfully.";
                    }
                    else
                    {
                        TempData["Error"] = "Department not found.";
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction("ManageDepartment");
        }
        
        [HttpGet]
        public IActionResult ChangeDepartmentStatus(long id)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            try
            {
                using (SqlConnection con = new SqlConnection(cs))
                {
                    con.Open();

                    // Get Current Status
                    SqlCommand checkCmd = new SqlCommand(
                        "SELECT IsActive FROM tbl_Department WHERE DepartmentId=@DepartmentId AND IsDeleted=0",
                        con);

                    checkCmd.Parameters.AddWithValue("@DepartmentId", id);

                    object result = checkCmd.ExecuteScalar();

                    if (result == null)
                    {
                        TempData["Error"] = "Department not found.";
                        return RedirectToAction("ManageDepartment");
                    }

                    bool currentStatus = Convert.ToBoolean(result);
                    bool newStatus = !currentStatus;

                    // Update Status
                    SqlCommand cmd = new SqlCommand(@"
                UPDATE tbl_Department
                SET IsActive=@IsActive,
                    UpdatedDate=GETDATE()
                WHERE DepartmentId=@DepartmentId", con);

                    cmd.Parameters.AddWithValue("@IsActive", newStatus);
                    cmd.Parameters.AddWithValue("@DepartmentId", id);

                    cmd.ExecuteNonQuery();

                    TempData["Success"] = newStatus
                        ? "Department activated successfully."
                        : "Department deactivated successfully.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction("ManageDepartment");
        }
        [HttpGet]
        public IActionResult ManageDoctor(string search = "", long departmentId = 0)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            List<DoctorModel> doctorList = new List<DoctorModel>();


            ViewBag.DepartmentList = new List<SelectListItem>();

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

                // Load Department Dropdown
                SqlCommand deptCmd = new SqlCommand(
                    "SELECT DepartmentId, DepartmentName FROM tbl_Department WHERE IsDeleted = 0 AND IsActive = 1 ORDER BY DepartmentName",
                    con);

                SqlDataReader deptDr = deptCmd.ExecuteReader();

                List<SelectListItem> departments = new List<SelectListItem>();

                departments.Add(new SelectListItem
                {
                    Text = "-- All Departments --",
                    Value = "0"
                });

                while (deptDr.Read())
                {
                    departments.Add(new SelectListItem
                    {
                        Value = deptDr["DepartmentId"].ToString(),
                        Text = deptDr["DepartmentName"].ToString()
                    });
                }

                deptDr.Close();

                ViewBag.DepartmentList = departments;

                // Doctor List
                SqlCommand cmd = new SqlCommand(@"
     SELECT
         D.DoctorId,
         D.DepartmentId,
         DP.DepartmentName,
         D.DoctorName,
         D.Qualification,
         D.Specialization,
         D.Experience,
         D.ConsultationFee,
         D.MobileNo,
         D.Email,
         D.Gender,
         D.DoctorImage,
         D.AvailableFrom,
         D.AvailableTo,
         D.Description,
         D.IsActive,
         D.CreatedDate,
         D.UpdatedDate
     FROM tbl_Doctor D
     INNER JOIN tbl_Department DP
         ON D.DepartmentId = DP.DepartmentId
     WHERE D.IsDeleted = 0
         AND (@Search = '' OR D.DoctorName LIKE '%' + @Search + '%')
         AND (@DepartmentId = 0 OR D.DepartmentId = @DepartmentId)
     ORDER BY D.DoctorId DESC", con);

                cmd.Parameters.AddWithValue("@Search", search);
                cmd.Parameters.AddWithValue("@DepartmentId", departmentId);

                SqlDataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    doctorList.Add(new DoctorModel
                    {
                        DoctorId = Convert.ToInt64(dr["DoctorId"]),
                        DepartmentId = Convert.ToInt64(dr["DepartmentId"]),
                        DepartmentName = dr["DepartmentName"].ToString(),
                        DoctorName = dr["DoctorName"].ToString(),
                        Qualification = dr["Qualification"].ToString(),
                        Specialization = dr["Specialization"].ToString(),
                        Experience = Convert.ToInt32(dr["Experience"]),
                        ConsultationFee = Convert.ToDecimal(dr["ConsultationFee"]),
                        MobileNo = dr["MobileNo"].ToString(),
                        Email = dr["Email"].ToString(),
                        Gender = dr["Gender"].ToString(),
                        DoctorImage = dr["DoctorImage"].ToString(),
                        AvailableFrom = dr["AvailableFrom"] != DBNull.Value
                            ? (TimeSpan?)dr.GetTimeSpan(dr.GetOrdinal("AvailableFrom"))
                            : null,
                        AvailableTo = dr["AvailableTo"] != DBNull.Value
                            ? (TimeSpan?)dr.GetTimeSpan(dr.GetOrdinal("AvailableTo"))
                            : null,
                        Description = dr["Description"].ToString(),
                        IsActive = Convert.ToBoolean(dr["IsActive"]),
                        CreatedDate = Convert.ToDateTime(dr["CreatedDate"]),
                        UpdatedDate = dr["UpdatedDate"] == DBNull.Value
                            ? null
                            : Convert.ToDateTime(dr["UpdatedDate"])
                    });
                }

                dr.Close();
            }

            ViewBag.Search = search;
            ViewBag.DepartmentId = departmentId;

            return View(doctorList);
        }
        [HttpGet]
        public IActionResult AddDoctor()
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            List<SelectListItem> departmentList = new List<SelectListItem>();

            using (SqlConnection con = new SqlConnection(cs))
            {
                SqlCommand cmd = new SqlCommand(@"
     SELECT DepartmentId, DepartmentName
     FROM tbl_Department
     WHERE IsDeleted = 0
       AND IsActive = 1
     ORDER BY DepartmentName", con);

                con.Open();

                SqlDataReader dr = cmd.ExecuteReader();

                departmentList.Add(new SelectListItem
                {
                    Text = "-- Select Department --",
                    Value = ""
                });

                while (dr.Read())
                {
                    departmentList.Add(new SelectListItem
                    {
                        Value = dr["DepartmentId"].ToString(),
                        Text = dr["DepartmentName"].ToString()
                    });
                }

                dr.Close();
            }

            ViewBag.DepartmentList = departmentList;

            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddDoctor(DoctorModel model)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            // Reload Department Dropdown
            List<SelectListItem> departmentList = new List<SelectListItem>();

            using (SqlConnection con = new SqlConnection(cs))
            {
                SqlCommand deptCmd = new SqlCommand(
                    "SELECT DepartmentId, DepartmentName FROM tbl_Department WHERE IsDeleted=0 AND IsActive=1 ORDER BY DepartmentName",
                    con);

                con.Open();

                SqlDataReader deptDr = deptCmd.ExecuteReader();

                departmentList.Add(new SelectListItem
                {
                    Text = "-- Select Department --",
                    Value = ""
                });

                while (deptDr.Read())
                {
                    departmentList.Add(new SelectListItem
                    {
                        Value = deptDr["DepartmentId"].ToString(),
                        Text = deptDr["DepartmentName"].ToString()
                    });
                }

                deptDr.Close();
            }

            ViewBag.DepartmentList = departmentList;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                using (SqlConnection con = new SqlConnection(cs))
                {
                    con.Open();

                    // Duplicate Check
                    SqlCommand checkCmd = new SqlCommand(@"
         SELECT COUNT(*)
         FROM tbl_Doctor
         WHERE DoctorName=@DoctorName
         AND DepartmentId=@DepartmentId
         AND IsDeleted=0", con);

                    checkCmd.Parameters.AddWithValue("@DoctorName", model.DoctorName);
                    checkCmd.Parameters.AddWithValue("@DepartmentId", model.DepartmentId);

                    int count = Convert.ToInt32(checkCmd.ExecuteScalar());

                    if (count > 0)
                    {
                        TempData["Error"] = "Doctor already exists in this department.";
                        return View(model);
                    }

                    // Image Upload
                    string fileName = "";

                    if (model.ImageFile != null)
                    {
                        string folder = Path.Combine(Directory.GetCurrentDirectory(),
                            "wwwroot/Department");

                        if (!Directory.Exists(folder))
                        {
                            Directory.CreateDirectory(folder);
                        }

                        fileName = Guid.NewGuid().ToString() +
                                   Path.GetExtension(model.ImageFile.FileName);

                        string filePath = Path.Combine(folder, fileName);

                        using (FileStream fs = new FileStream(filePath, FileMode.Create))
                        {
                            model.ImageFile.CopyTo(fs);
                        }
                    }

                    // Insert
                    SqlCommand cmd = new SqlCommand(@"
     INSERT INTO tbl_Doctor
     (
         DepartmentId,
         DoctorName,
         Qualification,
         Specialization,
         Experience,
         ConsultationFee,
         MobileNo,
         Email,
         Gender,
         DoctorImage,
         AvailableFrom,
         AvailableTo,
         Description,
         IsActive,
         IsDeleted,
         CreatedDate
     )
     VALUES
     (
         @DepartmentId,
         @DoctorName,
         @Qualification,
         @Specialization,
         @Experience,
         @ConsultationFee,
         @MobileNo,
         @Email,
         @Gender,
         @DoctorImage,
         @AvailableFrom,
         @AvailableTo,
         @Description,
         1,
         0,
         GETDATE()
     )", con);

                    cmd.Parameters.AddWithValue("@DepartmentId", model.DepartmentId);
                    cmd.Parameters.AddWithValue("@DoctorName", model.DoctorName);
                    cmd.Parameters.AddWithValue("@Qualification", model.Qualification);
                    cmd.Parameters.AddWithValue("@Specialization", model.Specialization);
                    cmd.Parameters.AddWithValue("@Experience", model.Experience);
                    cmd.Parameters.AddWithValue("@ConsultationFee", model.ConsultationFee);
                    cmd.Parameters.AddWithValue("@MobileNo", model.MobileNo);
                    cmd.Parameters.AddWithValue("@Email", model.Email);
                    cmd.Parameters.AddWithValue("@Gender", model.Gender);
                    cmd.Parameters.AddWithValue("@DoctorImage", fileName);
                    cmd.Parameters.AddWithValue("@AvailableFrom", model.AvailableFrom);
                    cmd.Parameters.AddWithValue("@AvailableTo", model.AvailableTo);
                    cmd.Parameters.AddWithValue("@Description",
                        (object?)model.Description ?? DBNull.Value);

                    cmd.ExecuteNonQuery();

                    TempData["Success"] = "Doctor added successfully.";
                }

                return RedirectToAction("ManageDoctor");
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return View(model);
            }
        }
        [HttpGet]
        public IActionResult EditDoctor(long id)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            DoctorModel model = new DoctorModel();


            List<SelectListItem> departmentList = new List<SelectListItem>();

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

                // Load Department Dropdown
                SqlCommand deptCmd = new SqlCommand(@"
     SELECT DepartmentId, DepartmentName
     FROM tbl_Department
     WHERE IsDeleted = 0
       AND IsActive = 1
     ORDER BY DepartmentName", con);

                SqlDataReader deptDr = deptCmd.ExecuteReader();

                departmentList.Add(new SelectListItem
                {
                    Text = "-- Select Department --",
                    Value = ""
                });

                while (deptDr.Read())
                {
                    departmentList.Add(new SelectListItem
                    {
                        Value = deptDr["DepartmentId"].ToString(),
                        Text = deptDr["DepartmentName"].ToString()
                    });
                }

                deptDr.Close();

                ViewBag.DepartmentList = departmentList;

                // Load Doctor Details
                SqlCommand cmd = new SqlCommand(@"
     SELECT *
     FROM tbl_Doctor
     WHERE DoctorId = @DoctorId
       AND IsDeleted = 0", con);

                cmd.Parameters.AddWithValue("@DoctorId", id);

                SqlDataReader dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    model.DoctorId = Convert.ToInt64(dr["DoctorId"]);
                    model.DepartmentId = Convert.ToInt64(dr["DepartmentId"]);
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
                        ? (TimeSpan?)dr.GetTimeSpan(dr.GetOrdinal("AvailableFrom"))
                        : null;

                    model.AvailableTo = dr["AvailableTo"] != DBNull.Value
                        ? (TimeSpan?)dr.GetTimeSpan(dr.GetOrdinal("AvailableTo"))
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
                    dr.Close();
                    TempData["Error"] = "Doctor not found.";
                    return RedirectToAction("ManageDoctor");
                }

                dr.Close();
            }

            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditDoctor(DoctorModel model)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }


            List<SelectListItem> departmentList = new List<SelectListItem>();

            using (SqlConnection con = new SqlConnection(cs))
            {
                SqlCommand deptCmd = new SqlCommand(
                    "SELECT DepartmentId, DepartmentName FROM tbl_Department WHERE IsDeleted = 0 AND IsActive = 1 ORDER BY DepartmentName",
                    con);

                con.Open();

                SqlDataReader deptDr = deptCmd.ExecuteReader();

                departmentList.Add(new SelectListItem
                {
                    Text = "-- Select Department --",
                    Value = ""
                });

                while (deptDr.Read())
                {
                    departmentList.Add(new SelectListItem
                    {
                        Value = deptDr["DepartmentId"].ToString(),
                        Text = deptDr["DepartmentName"].ToString()
                    });
                }

                deptDr.Close();
            }

            ViewBag.DepartmentList = departmentList;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                using (SqlConnection con = new SqlConnection(cs))
                {
                    con.Open();


                    SqlCommand checkCmd = new SqlCommand(@"
         SELECT COUNT(*)
         FROM tbl_Doctor
         WHERE DoctorName = @DoctorName
         AND DepartmentId = @DepartmentId
         AND DoctorId <> @DoctorId
         AND IsDeleted = 0", con);

                    checkCmd.Parameters.AddWithValue("@DoctorName", model.DoctorName);
                    checkCmd.Parameters.AddWithValue("@DepartmentId", model.DepartmentId);
                    checkCmd.Parameters.AddWithValue("@DoctorId", model.DoctorId);

                    int count = Convert.ToInt32(checkCmd.ExecuteScalar());

                    if (count > 0)
                    {
                        TempData["Error"] = "Doctor already exists in this department.";
                        return View(model);
                    }


                    string oldImage = "";

                    SqlCommand imgCmd = new SqlCommand(
                        "SELECT DoctorImage FROM tbl_Doctor WHERE DoctorId=@DoctorId",
                        con);

                    imgCmd.Parameters.AddWithValue("@DoctorId", model.DoctorId);

                    object img = imgCmd.ExecuteScalar();

                    if (img != null)
                    {
                        oldImage = img.ToString();
                    }

                    string imageName = oldImage;


                    if (model.ImageFile != null)
                    {
                        string folder = Path.Combine(
                            Directory.GetCurrentDirectory(),
                            "wwwroot/Department");

                        if (!Directory.Exists(folder))
                        {
                            Directory.CreateDirectory(folder);
                        }


                        if (!string.IsNullOrEmpty(oldImage))
                        {
                            string oldPath = Path.Combine(folder, oldImage);

                            if (System.IO.File.Exists(oldPath))
                            {
                                System.IO.File.Delete(oldPath);
                            }
                        }

                        imageName = Guid.NewGuid().ToString()
                                    + Path.GetExtension(model.ImageFile.FileName);

                        string newPath = Path.Combine(folder, imageName);

                        using (FileStream fs = new FileStream(newPath, FileMode.Create))
                        {
                            model.ImageFile.CopyTo(fs);
                        }
                    }


                    SqlCommand cmd = new SqlCommand(@"
         UPDATE tbl_Doctor
         SET
             DepartmentId=@DepartmentId,
             DoctorName=@DoctorName,
             Qualification=@Qualification,
             Specialization=@Specialization,
             Experience=@Experience,
             ConsultationFee=@ConsultationFee,
             MobileNo=@MobileNo,
             Email=@Email,
             Gender=@Gender,
             DoctorImage=@DoctorImage,
             AvailableFrom=@AvailableFrom,
             AvailableTo=@AvailableTo,
             Description=@Description,
             UpdatedDate=GETDATE()
         WHERE DoctorId=@DoctorId", con);

                    cmd.Parameters.AddWithValue("@DepartmentId", model.DepartmentId);
                    cmd.Parameters.AddWithValue("@DoctorName", model.DoctorName);
                    cmd.Parameters.AddWithValue("@Qualification", model.Qualification);
                    cmd.Parameters.AddWithValue("@Specialization", model.Specialization);
                    cmd.Parameters.AddWithValue("@Experience", model.Experience);
                    cmd.Parameters.AddWithValue("@ConsultationFee", model.ConsultationFee);
                    cmd.Parameters.AddWithValue("@MobileNo", model.MobileNo);
                    cmd.Parameters.AddWithValue("@Email", model.Email);
                    cmd.Parameters.AddWithValue("@Gender", model.Gender);
                    cmd.Parameters.AddWithValue("@DoctorImage", imageName);
                    cmd.Parameters.AddWithValue("@AvailableFrom", model.AvailableFrom);
                    cmd.Parameters.AddWithValue("@AvailableTo", model.AvailableTo);
                    cmd.Parameters.AddWithValue("@Description",
                        (object?)model.Description ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@DoctorId", model.DoctorId);

                    cmd.ExecuteNonQuery();

                    TempData["Success"] = "Doctor updated successfully.";
                }

                return RedirectToAction("ManageDoctor");
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return View(model);
            }
        }
        [HttpGet]
        public IActionResult ViewDoctor(long id)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            DoctorModel model = new DoctorModel();

            using (SqlConnection con = new SqlConnection(cs))
            {
                SqlCommand cmd = new SqlCommand(@"
     SELECT
         D.*,
         DP.DepartmentName
     FROM tbl_Doctor D
     INNER JOIN tbl_Department DP
         ON D.DepartmentId = DP.DepartmentId
     WHERE D.DoctorId = @DoctorId
     AND D.IsDeleted = 0", con);

                cmd.Parameters.AddWithValue("@DoctorId", id);

                con.Open();

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
                    model.Description = dr["Description"].ToString();

                    model.AvailableFrom = dr["AvailableFrom"] != DBNull.Value
                        ? (TimeSpan?)dr.GetTimeSpan(dr.GetOrdinal("AvailableFrom"))
                        : null;

                    model.AvailableTo = dr["AvailableTo"] != DBNull.Value
                        ? (TimeSpan?)dr.GetTimeSpan(dr.GetOrdinal("AvailableTo"))
                        : null;

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
                    TempData["Error"] = "Doctor not found.";
                    return RedirectToAction("ManageDoctor");
                }

                dr.Close();
            }

            return View(model);
        }
        [HttpGet]
        public IActionResult DeleteDoctor(long id)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            try
            {
                using (SqlConnection con = new SqlConnection(cs))
                {
                    con.Open();


                    SqlCommand checkCmd = new SqlCommand(@"
         SELECT COUNT(*)
         FROM tbl_Doctor
         WHERE DoctorId = @DoctorId
         AND IsDeleted = 0", con);

                    checkCmd.Parameters.AddWithValue("@DoctorId", id);

                    int count = Convert.ToInt32(checkCmd.ExecuteScalar());

                    if (count == 0)
                    {
                        TempData["Error"] = "Doctor not found.";
                        return RedirectToAction("ManageDoctor");
                    }

                    // Soft Delete
                    SqlCommand cmd = new SqlCommand(@"
         UPDATE tbl_Doctor
         SET
             IsDeleted = 1,
             UpdatedDate = GETDATE()
         WHERE DoctorId = @DoctorId", con);

                    cmd.Parameters.AddWithValue("@DoctorId", id);

                    cmd.ExecuteNonQuery();

                    TempData["Success"] = "Doctor deleted successfully.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction("ManageDoctor");
        }
        [HttpGet]
        public IActionResult ChangeDoctorStatus(long id)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            try
            {
                using (SqlConnection con = new SqlConnection(cs))
                {
                    con.Open();

                    // Check Doctor Exists & Get Current Status
                    SqlCommand checkCmd = new SqlCommand(@"
         SELECT IsActive
         FROM tbl_Doctor
         WHERE DoctorId = @DoctorId
           AND IsDeleted = 0", con);

                    checkCmd.Parameters.AddWithValue("@DoctorId", id);

                    object result = checkCmd.ExecuteScalar();

                    if (result == null)
                    {
                        TempData["Error"] = "Doctor not found.";
                        return RedirectToAction("ManageDoctor");
                    }

                    bool currentStatus = Convert.ToBoolean(result);
                    bool newStatus = !currentStatus;

                    // Update Status
                    SqlCommand cmd = new SqlCommand(@"
         UPDATE tbl_Doctor
         SET
             IsActive = @IsActive,
             UpdatedDate = GETDATE()
         WHERE DoctorId = @DoctorId", con);

                    cmd.Parameters.AddWithValue("@IsActive", newStatus);
                    cmd.Parameters.AddWithValue("@DoctorId", id);

                    cmd.ExecuteNonQuery();

                    if (newStatus)
                    {
                        TempData["Success"] = "Doctor activated successfully.";
                    }
                    else
                    {
                        TempData["Success"] = "Doctor deactivated successfully.";
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction("ManageDoctor");
        }
        // ==========================================
        // Manage Patients (Admin)
        // ==========================================
        [HttpGet]
        public IActionResult ManagePatient(string search = "")
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            List<CustomerModel> patientList = new List<CustomerModel>();

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();
                string query = @"
                    SELECT *
                    FROM tbl_Customer
                    WHERE IsDeleted = 0
                      AND (@Search = '' OR FullName LIKE '%' + @Search + '%' OR MobileNo LIKE '%' + @Search + '%' OR Email LIKE '%' + @Search + '%')
                    ORDER BY CustomerId DESC";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@Search", search);

                SqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    patientList.Add(new CustomerModel
                    {
                        CustomerId = Convert.ToInt64(dr["CustomerId"]),
                        FullName = dr["FullName"] != DBNull.Value ? dr["FullName"].ToString() : "",
                        Gender = dr["Gender"] != DBNull.Value ? dr["Gender"].ToString() : "",
                        DateOfBirth = dr["DateOfBirth"] != DBNull.Value ? Convert.ToDateTime(dr["DateOfBirth"]) : null,
                        BloodGroup = dr["BloodGroup"] != DBNull.Value ? dr["BloodGroup"].ToString() : "",
                        MobileNo = dr["MobileNo"] != DBNull.Value ? dr["MobileNo"].ToString() : "",
                        Email = dr["Email"] != DBNull.Value ? dr["Email"].ToString() : "",
                        Address = dr["Address"] != DBNull.Value ? dr["Address"].ToString() : "",
                        City = dr["City"] != DBNull.Value ? dr["City"].ToString() : "",
                        State = dr["State"] != DBNull.Value ? dr["State"].ToString() : "",
                        Pincode = dr["Pincode"] != DBNull.Value ? dr["Pincode"].ToString() : "",
                        ProfileImage = dr["ProfileImage"] != DBNull.Value ? dr["ProfileImage"].ToString() : "",
                        IsActive = dr["IsActive"] != DBNull.Value && Convert.ToBoolean(dr["IsActive"]),
                        CreatedDate = dr["CreatedDate"] != DBNull.Value ? Convert.ToDateTime(dr["CreatedDate"]) : DateTime.Now,
                        UpdatedDate = dr["UpdatedDate"] != DBNull.Value ? Convert.ToDateTime(dr["UpdatedDate"]) : null
                    });
                }
                dr.Close();
            }

            ViewBag.Search = search;
            return View(patientList);
        }

        // ==========================================
        // View Patient Details (Admin)
        // ==========================================
        [HttpGet]
        public IActionResult ViewPatient(long id)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            CustomerModel model = new CustomerModel();

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();
                SqlCommand cmd = new SqlCommand("SELECT * FROM tbl_Customer WHERE CustomerId = @CustomerId AND IsDeleted = 0", con);
                cmd.Parameters.AddWithValue("@CustomerId", id);

                SqlDataReader dr = cmd.ExecuteReader();
                if (dr.Read())
                {
                    model.CustomerId = Convert.ToInt64(dr["CustomerId"]);
                    model.FullName = dr["FullName"] != DBNull.Value ? dr["FullName"].ToString() : "";
                    model.Gender = dr["Gender"] != DBNull.Value ? dr["Gender"].ToString() : "";
                    model.DateOfBirth = dr["DateOfBirth"] != DBNull.Value ? Convert.ToDateTime(dr["DateOfBirth"]) : null;
                    model.BloodGroup = dr["BloodGroup"] != DBNull.Value ? dr["BloodGroup"].ToString() : "";
                    model.MobileNo = dr["MobileNo"] != DBNull.Value ? dr["MobileNo"].ToString() : "";
                    model.Email = dr["Email"] != DBNull.Value ? dr["Email"].ToString() : "";
                    model.Address = dr["Address"] != DBNull.Value ? dr["Address"].ToString() : "";
                    model.City = dr["City"] != DBNull.Value ? dr["City"].ToString() : "";
                    model.State = dr["State"] != DBNull.Value ? dr["State"].ToString() : "";
                    model.Pincode = dr["Pincode"] != DBNull.Value ? dr["Pincode"].ToString() : "";
                    model.ProfileImage = dr["ProfileImage"] != DBNull.Value ? dr["ProfileImage"].ToString() : "";
                    model.IsActive = dr["IsActive"] != DBNull.Value && Convert.ToBoolean(dr["IsActive"]);
                    model.CreatedDate = dr["CreatedDate"] != DBNull.Value ? Convert.ToDateTime(dr["CreatedDate"]) : DateTime.Now;
                    if (dr["UpdatedDate"] != DBNull.Value)
                    {
                        model.UpdatedDate = Convert.ToDateTime(dr["UpdatedDate"]);
                    }
                }
                else
                {
                    dr.Close();
                    TempData["Error"] = "Patient record not found.";
                    return RedirectToAction("ManagePatient");
                }
                dr.Close();
            }

            return View(model);
        }

        // ==========================================
        // Delete Patient (Admin - Soft Delete)
        // ==========================================
        [HttpGet]
        public IActionResult DeletePatient(long id)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            try
            {
                using (SqlConnection con = new SqlConnection(cs))
                {
                    con.Open();
                    SqlCommand cmd = new SqlCommand(@"
                        UPDATE tbl_Customer 
                        SET IsDeleted = 1, UpdatedDate = GETDATE() 
                        WHERE CustomerId = @CustomerId", con);

                    cmd.Parameters.AddWithValue("@CustomerId", id);

                    int result = cmd.ExecuteNonQuery();
                    if (result > 0)
                    {
                        TempData["Success"] = "Patient deleted successfully.";
                    }
                    else
                    {
                        TempData["Error"] = "Patient record not found.";
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error deleting patient: " + ex.Message;
            }

            return RedirectToAction("ManagePatient");
        }

        // ==========================================
        // Change Patient Status (Active / Inactive)
        // ==========================================
        [HttpGet]
        public IActionResult ChangePatientStatus(long id)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            try
            {
                using (SqlConnection con = new SqlConnection(cs))
                {
                    con.Open();
                    SqlCommand checkCmd = new SqlCommand("SELECT IsActive FROM tbl_Customer WHERE CustomerId = @CustomerId AND IsDeleted = 0", con);
                    checkCmd.Parameters.AddWithValue("@CustomerId", id);

                    object result = checkCmd.ExecuteScalar();
                    if (result == null)
                    {
                        TempData["Error"] = "Patient record not found.";
                        return RedirectToAction("ManagePatient");
                    }

                    bool currentStatus = Convert.ToBoolean(result);
                    bool newStatus = !currentStatus;

                    SqlCommand cmd = new SqlCommand("UPDATE tbl_Customer SET IsActive = @IsActive, UpdatedDate = GETDATE() WHERE CustomerId = @CustomerId", con);
                    cmd.Parameters.AddWithValue("@IsActive", newStatus);
                    cmd.Parameters.AddWithValue("@CustomerId", id);
                    cmd.ExecuteNonQuery();

                    TempData["Success"] = newStatus ? "Patient activated successfully." : "Patient deactivated successfully.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction("ManagePatient");
        }
        // ==========================================
        // GET : Manage Appointments
        // ==========================================
        [HttpGet]
        public IActionResult ManageAppointments(string search = "", string status = "")
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            List<AppointmentModel> list = new List<AppointmentModel>();

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

                string query = @"
SELECT
    a.AppointmentId,
    a.AppointmentNo,
    a.AppointmentDate,
    a.AppointmentTime,
    a.ConsultationFee,
    a.AppointmentStatus,
    a.PaymentStatus,
    a.CreatedDate,

    c.FullName AS CustomerName,
    c.MobileNo,

    d.DoctorName,

    dep.DepartmentName

FROM tbl_Appointment a

INNER JOIN tbl_Customer c
    ON a.CustomerId = c.CustomerId

INNER JOIN tbl_Doctor d
    ON a.DoctorId = d.DoctorId

INNER JOIN tbl_Department dep
    ON d.DepartmentId = dep.DepartmentId

WHERE a.IsDeleted = 0

AND (@Status = '' OR a.AppointmentStatus = @Status)

AND
(
    a.AppointmentNo LIKE @Search
    OR c.FullName LIKE @Search
    OR d.DoctorName LIKE @Search
)

ORDER BY

CASE
    WHEN a.AppointmentStatus='Pending' THEN 1
    WHEN a.AppointmentStatus='Approved' THEN 2
    WHEN a.AppointmentStatus='Confirmed' THEN 3
    WHEN a.AppointmentStatus='Completed' THEN 4
    WHEN a.AppointmentStatus='Cancelled' THEN 5
    WHEN a.AppointmentStatus='Rejected' THEN 6
    ELSE 7
END,

a.CreatedDate DESC";

                SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@Search", "%" + search + "%");
                cmd.Parameters.AddWithValue("@Status", status ?? "");

                SqlDataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    AppointmentModel model = new AppointmentModel();

                    model.AppointmentId = Convert.ToInt64(dr["AppointmentId"]);
                    model.AppointmentNo = dr["AppointmentNo"].ToString();

                    if (dr["AppointmentDate"] != DBNull.Value)
                        model.AppointmentDate = Convert.ToDateTime(dr["AppointmentDate"]);

                    if (dr["AppointmentTime"] != DBNull.Value)
                        model.AppointmentTime = (TimeSpan)dr["AppointmentTime"];

                    model.CustomerName = dr["CustomerName"].ToString();
                    model.MobileNo = dr["MobileNo"].ToString();
                    model.DoctorName = dr["DoctorName"].ToString();
                    model.Department = dr["DepartmentName"].ToString();
                    model.ConsultationFee = Convert.ToDecimal(dr["ConsultationFee"]);
                    model.AppointmentStatus = dr["AppointmentStatus"].ToString();
                    model.PaymentStatus = dr["PaymentStatus"].ToString();
                    model.CreatedDate = Convert.ToDateTime(dr["CreatedDate"]);

                    list.Add(model);
                }

                dr.Close();
            }

            ViewBag.Search = search;
            ViewBag.Status = status;

            return View(list);
        }
        // ==========================================
        // GET : View Appointment
        // ==========================================
        [HttpGet]
        public IActionResult ViewAppointment(long id)
        {
            // Admin Login Check
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            AppointmentModel model = new AppointmentModel();

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

                string query = @"
SELECT

    a.*,

    c.FullName AS CustomerName,
    c.MobileNo,
    c.Email,

    d.DoctorName,
    d.Qualification,
    d.Specialization,
    d.Experience,
    d.DoctorImage,
    d.AvailableFrom,
    d.AvailableTo,

    dep.DepartmentName

FROM tbl_Appointment a

INNER JOIN tbl_Customer c
    ON a.CustomerId = c.CustomerId

INNER JOIN tbl_Doctor d
    ON a.DoctorId = d.DoctorId

INNER JOIN tbl_Department dep
    ON d.DepartmentId = dep.DepartmentId

WHERE a.AppointmentId = @AppointmentId
AND a.IsDeleted = 0";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@AppointmentId", id);

                SqlDataReader dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    model.AppointmentId = Convert.ToInt64(dr["AppointmentId"]);
                    model.AppointmentNo = dr["AppointmentNo"].ToString();

                    model.CustomerId = Convert.ToInt64(dr["CustomerId"]);
                    model.DoctorId = Convert.ToInt64(dr["DoctorId"]);

                    model.CustomerName = dr["CustomerName"].ToString();
                    model.MobileNo = dr["MobileNo"].ToString();
                    model.Email = dr["Email"].ToString();

                    model.DoctorName = dr["DoctorName"].ToString();
                    model.DepartmentName = dr["DepartmentName"].ToString();

                    model.Qualification = dr["Qualification"].ToString();
                    model.Specialization = dr["Specialization"].ToString();
                    model.Experience = dr["Experience"].ToString();
                    model.DoctorImage = dr["DoctorImage"].ToString();

                    if (dr["AvailableFrom"] != DBNull.Value)
                        model.AvailableFrom = (TimeSpan)dr["AvailableFrom"];

                    if (dr["AvailableTo"] != DBNull.Value)
                        model.AvailableTo = (TimeSpan)dr["AvailableTo"];

                    if (dr["AppointmentDate"] != DBNull.Value)
                        model.AppointmentDate = Convert.ToDateTime(dr["AppointmentDate"]);

                    if (dr["AppointmentTime"] != DBNull.Value)
                        model.AppointmentTime = (TimeSpan)dr["AppointmentTime"];

                    model.Symptoms = dr["Symptoms"].ToString();

                    if (dr["ConsultationFee"] != DBNull.Value)
                        model.ConsultationFee = Convert.ToDecimal(dr["ConsultationFee"]);

                    model.AppointmentStatus = dr["AppointmentStatus"].ToString();
                    model.PaymentStatus = dr["PaymentStatus"].ToString();

                    model.AdminRemark = dr["AdminRemark"] == DBNull.Value ? "" : dr["AdminRemark"].ToString();
                    model.CustomerRemark = dr["CustomerRemark"] == DBNull.Value ? "" : dr["CustomerRemark"].ToString();
                    model.PrescriptionFile = dr["PrescriptionFile"] == DBNull.Value ? "" : dr["PrescriptionFile"].ToString();

                    model.IsActive = Convert.ToBoolean(dr["IsActive"]);
                    model.IsDeleted = Convert.ToBoolean(dr["IsDeleted"]);

                    model.CreatedDate = Convert.ToDateTime(dr["CreatedDate"]);

                    if (dr["UpdatedDate"] != DBNull.Value)
                        model.UpdatedDate = Convert.ToDateTime(dr["UpdatedDate"]);
                }

                dr.Close();
            }

            if (model.AppointmentId == 0)
            {
                TempData["Error"] = "Appointment not found.";
                return RedirectToAction("ManageAppointments");
            }

            return View(model);
        }
        // ==========================================
        // GET : Edit Appointment
        // ==========================================
        [HttpGet]
        public IActionResult EditAppointment(long id)
        {
            // Admin Login Check
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            AppointmentModel model = new AppointmentModel();

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

                string query = @"
SELECT

    a.*,

    c.FullName AS CustomerName,
    c.MobileNo,
    c.Email,

    d.DoctorName,
    d.DoctorImage,
    d.Qualification,
    d.Specialization,
    d.Experience,

    dep.DepartmentName

FROM tbl_Appointment a

INNER JOIN tbl_Customer c
    ON a.CustomerId = c.CustomerId

INNER JOIN tbl_Doctor d
    ON a.DoctorId = d.DoctorId

INNER JOIN tbl_Department dep
    ON d.DepartmentId = dep.DepartmentId

WHERE a.AppointmentId = @AppointmentId
AND a.IsDeleted = 0";

                SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@AppointmentId", id);

                SqlDataReader dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    model.AppointmentId = Convert.ToInt64(dr["AppointmentId"]);

                    model.AppointmentNo = dr["AppointmentNo"].ToString();

                    model.CustomerId = Convert.ToInt64(dr["CustomerId"]);

                    model.DoctorId = Convert.ToInt64(dr["DoctorId"]);

                    model.CustomerName = dr["CustomerName"].ToString();

                    model.MobileNo = dr["MobileNo"].ToString();

                    model.Email = dr["Email"].ToString();

                    model.DoctorName = dr["DoctorName"].ToString();

                    model.DepartmentName = dr["DepartmentName"].ToString();

                    model.DoctorImage = dr["DoctorImage"].ToString();

                    model.Qualification = dr["Qualification"].ToString();

                    model.Specialization = dr["Specialization"].ToString();

                    model.Experience = dr["Experience"].ToString();

                    if (dr["AppointmentDate"] != DBNull.Value)
                        model.AppointmentDate = Convert.ToDateTime(dr["AppointmentDate"]);

                    if (dr["AppointmentTime"] != DBNull.Value)
                        model.AppointmentTime = (TimeSpan?)dr["AppointmentTime"];

                    model.Symptoms = dr["Symptoms"].ToString();

                    model.ConsultationFee = Convert.ToDecimal(dr["ConsultationFee"]);

                    model.AppointmentStatus = dr["AppointmentStatus"].ToString();

                    model.PaymentStatus = dr["PaymentStatus"].ToString();

                    model.AdminRemark = dr["AdminRemark"].ToString();

                    model.CustomerRemark = dr["CustomerRemark"].ToString();

                    model.CreatedDate = Convert.ToDateTime(dr["CreatedDate"]);
                }

                dr.Close();
            }

            if (model.AppointmentId == 0)
            {
                TempData["Error"] = "Appointment not found.";

                return RedirectToAction("ManageAppointments");
            }

            // Status Dropdown
            ViewBag.StatusList = new List<SelectListItem>()
    {
        new SelectListItem(){ Text="Pending", Value="Pending"},
        new SelectListItem(){ Text="Approved", Value="Approved"},
        new SelectListItem(){ Text="Confirmed", Value="Confirmed"},
        new SelectListItem(){ Text="Completed", Value="Completed"},
        new SelectListItem(){ Text="Rejected", Value="Rejected"},
        new SelectListItem(){ Text="Cancelled", Value="Cancelled"}
    };

            return View(model);
        }
        // ==========================================
        // POST : Edit Appointment
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditAppointment(AppointmentModel model)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            // Remove unwanted validation
            ModelState.Remove("CustomerName");
            ModelState.Remove("DoctorName");
            ModelState.Remove("Department");
            ModelState.Remove("DepartmentName");
            ModelState.Remove("Qualification");
            ModelState.Remove("Specialization");
            ModelState.Remove("Experience");
            ModelState.Remove("DoctorImage");
            ModelState.Remove("MobileNo");
            ModelState.Remove("Email");
            ModelState.Remove("Prescription");
            ModelState.Remove("PrescriptionFile");
            ModelState.Remove("CustomerRemark");
            ModelState.Remove("AvailableFrom");
            ModelState.Remove("AvailableTo");
            ModelState.Remove("CustomerId");
            ModelState.Remove("DoctorId");
            ModelState.Remove("AppointmentNo");
            ModelState.Remove("Symptoms");
            ModelState.Remove("PaymentStatus");
            ModelState.Remove("CreatedDate");
            ModelState.Remove("UpdatedDate");

            if (!ModelState.IsValid)
            {
                ViewBag.StatusList = new List<SelectListItem>()
        {
            new SelectListItem(){ Text="Pending", Value="Pending"},
            new SelectListItem(){ Text="Approved", Value="Approved"},
            new SelectListItem(){ Text="Confirmed", Value="Confirmed"},
            new SelectListItem(){ Text="Completed", Value="Completed"},
            new SelectListItem(){ Text="Rejected", Value="Rejected"},
            new SelectListItem(){ Text="Cancelled", Value="Cancelled"}
        };

                return View(model);
            }

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

                string query = @"UPDATE tbl_Appointment
                         SET
                            AppointmentDate=@AppointmentDate,
                            AppointmentTime=@AppointmentTime,
                            ConsultationFee=@ConsultationFee,
                            AppointmentStatus=@AppointmentStatus,
                            AdminRemark=@AdminRemark,
                            UpdatedDate=@UpdatedDate
                         WHERE AppointmentId=@AppointmentId";

                SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@AppointmentId", model.AppointmentId);

                if (model.AppointmentDate != null)
                    cmd.Parameters.AddWithValue("@AppointmentDate", model.AppointmentDate);
                else
                    cmd.Parameters.AddWithValue("@AppointmentDate", DBNull.Value);

                if (model.AppointmentTime != null)
                    cmd.Parameters.AddWithValue("@AppointmentTime", model.AppointmentTime);
                else
                    cmd.Parameters.AddWithValue("@AppointmentTime", DBNull.Value);

                cmd.Parameters.AddWithValue("@ConsultationFee", model.ConsultationFee);

                cmd.Parameters.AddWithValue("@AppointmentStatus", model.AppointmentStatus);

                if (!string.IsNullOrWhiteSpace(model.AdminRemark))
                    cmd.Parameters.AddWithValue("@AdminRemark", model.AdminRemark);
                else
                    cmd.Parameters.AddWithValue("@AdminRemark", DBNull.Value);

                cmd.Parameters.AddWithValue("@UpdatedDate", DateTime.Now);

                int result = cmd.ExecuteNonQuery();

                if (result > 0)
                {
                    TempData["Success"] = "Appointment updated successfully.";
                    return RedirectToAction("ManageAppointments");
                }
                else
                {
                    TempData["Error"] = "Appointment update failed.";
                }
            }

            ViewBag.StatusList = new List<SelectListItem>()
    {
        new SelectListItem(){ Text="Pending", Value="Pending"},
        new SelectListItem(){ Text="Approved", Value="Approved"},
        new SelectListItem(){ Text="Confirmed", Value="Confirmed"},
        new SelectListItem(){ Text="Completed", Value="Completed"},
        new SelectListItem(){ Text="Rejected", Value="Rejected"},
        new SelectListItem(){ Text="Cancelled", Value="Cancelled"}
    };

            return View(model);
        }
       
        [HttpGet]
        public IActionResult ApproveAppointment(long id)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

                // Check Appointment Status
                string checkQuery = @"
        SELECT AppointmentStatus
        FROM tbl_Appointment
        WHERE AppointmentId=@AppointmentId
        AND IsDeleted=0";

                SqlCommand checkCmd = new SqlCommand(checkQuery, con);

                checkCmd.Parameters.AddWithValue("@AppointmentId", id);

                object statusObj = checkCmd.ExecuteScalar();

                if (statusObj == null)
                {
                    TempData["Error"] = "Appointment not found.";

                    return RedirectToAction("ManageAppointments");
                }

                string status = statusObj.ToString();

                if (status == "Approved")
                {
                    TempData["Error"] = "Appointment is already approved.";

                    return RedirectToAction("ManageAppointments");
                }

                if (status == "Rejected")
                {
                    TempData["Error"] = "Rejected appointment cannot be approved.";

                    return RedirectToAction("ManageAppointments");
                }

                if (status == "Cancelled")
                {
                    TempData["Error"] = "Cancelled appointment cannot be approved.";

                    return RedirectToAction("ManageAppointments");
                }

                if (status == "Completed")
                {
                    TempData["Error"] = "Completed appointment cannot be approved.";

                    return RedirectToAction("ManageAppointments");
                }

                string query = @"
        UPDATE tbl_Appointment
        SET

            AppointmentStatus=@AppointmentStatus,
            UpdatedDate=@UpdatedDate

        WHERE AppointmentId=@AppointmentId";

                SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@AppointmentStatus", "Approved");
                cmd.Parameters.AddWithValue("@UpdatedDate", DateTime.Now);
                cmd.Parameters.AddWithValue("@AppointmentId", id);

                int result = cmd.ExecuteNonQuery();

                if (result > 0)
                {
                    TempData["Success"] = "Appointment approved successfully.";
                }
                else
                {
                    TempData["Error"] = "Unable to approve appointment.";
                }
            }

            return RedirectToAction("ManageAppointments");
        }
        
        [HttpGet]
        public IActionResult RejectAppointment(long id)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

                
                string checkQuery = @"
        SELECT AppointmentStatus
        FROM tbl_Appointment
        WHERE AppointmentId=@AppointmentId
        AND IsDeleted=0";

                SqlCommand checkCmd = new SqlCommand(checkQuery, con);

                checkCmd.Parameters.AddWithValue("@AppointmentId", id);

                object statusObj = checkCmd.ExecuteScalar();

                if (statusObj == null)
                {
                    TempData["Error"] = "Appointment not found.";

                    return RedirectToAction("ManageAppointments");
                }

                string status = statusObj.ToString();

              
                if (status == "Rejected")
                {
                    TempData["Error"] = "Appointment is already rejected.";

                    return RedirectToAction("ManageAppointments");
                }

               
                if (status == "Approved")
                {
                    TempData["Error"] = "Approved appointment cannot be rejected.";

                    return RedirectToAction("ManageAppointments");
                }

               
                if (status == "Completed")
                {
                    TempData["Error"] = "Completed appointment cannot be rejected.";

                    return RedirectToAction("ManageAppointments");
                }

               
                if (status == "Cancelled")
                {
                    TempData["Error"] = "Cancelled appointment cannot be rejected.";

                    return RedirectToAction("ManageAppointments");
                }

               
                string query = @"
        UPDATE tbl_Appointment
        SET

            AppointmentStatus=@AppointmentStatus,
            UpdatedDate=@UpdatedDate

        WHERE AppointmentId=@AppointmentId";

                SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@AppointmentStatus", "Rejected");
                cmd.Parameters.AddWithValue("@UpdatedDate", DateTime.Now);
                cmd.Parameters.AddWithValue("@AppointmentId", id);

                int result = cmd.ExecuteNonQuery();

                if (result > 0)
                {
                    TempData["Success"] = "Appointment rejected successfully.";
                }
                else
                {
                    TempData["Error"] = "Unable to reject appointment.";
                }
            }

            return RedirectToAction("ManageAppointments");
        }
        
        [HttpGet]
        public IActionResult UploadPrescription(long id)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            AppointmentModel model = new AppointmentModel();

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

                string query = @"
SELECT

    a.AppointmentId,
    a.AppointmentNo,
    a.AppointmentDate,
    a.AppointmentTime,
    a.PrescriptionFile,
    a.AppointmentStatus,

    c.FullName AS CustomerName,

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

WHERE a.AppointmentId = @AppointmentId
AND a.IsDeleted = 0";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@AppointmentId", id);

                SqlDataReader dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    model.AppointmentId = Convert.ToInt64(dr["AppointmentId"]);
                    model.AppointmentNo = dr["AppointmentNo"].ToString();

                    model.CustomerName = dr["CustomerName"].ToString();

                    model.DoctorName = dr["DoctorName"].ToString();
                    model.DepartmentName = dr["DepartmentName"].ToString();
                    model.DoctorImage = dr["DoctorImage"].ToString();

                    model.AppointmentStatus = dr["AppointmentStatus"].ToString();

                    model.PrescriptionFile = dr["PrescriptionFile"] == DBNull.Value
                        ? ""
                        : dr["PrescriptionFile"].ToString();

                    if (dr["AppointmentDate"] != DBNull.Value)
                        model.AppointmentDate = Convert.ToDateTime(dr["AppointmentDate"]);

                    if (dr["AppointmentTime"] != DBNull.Value)
                        model.AppointmentTime = (TimeSpan)dr["AppointmentTime"];
                }

                dr.Close();
            }

            if (model.AppointmentId == 0)
            {
                TempData["Error"] = "Appointment not found.";
                return RedirectToAction("ManageAppointments");
            }

           
            if (model.AppointmentStatus != "Approved")
            {
                TempData["Error"] = "Prescription can only be uploaded for approved appointments.";
                return RedirectToAction("ManageAppointments");
            }

            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UploadPrescription(AppointmentModel model)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            ModelState.Remove("DoctorName");
            ModelState.Remove("CustomerName");
            ModelState.Remove("DepartmentName");
            ModelState.Remove("DoctorImage");
            ModelState.Remove("AppointmentNo");
            ModelState.Remove("AppointmentStatus");
            ModelState.Remove("PrescriptionFile");

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string fileName = "";

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

                SqlCommand cmdOld = new SqlCommand("SELECT PrescriptionFile FROM tbl_Appointment WHERE AppointmentId=@AppointmentId", con);
                cmdOld.Parameters.AddWithValue("@AppointmentId", model.AppointmentId);

                object oldFile = cmdOld.ExecuteScalar();

                if (oldFile != null && oldFile != DBNull.Value)
                {
                    fileName = oldFile.ToString();
                }

                if (model.Prescription != null && model.Prescription.Length > 0)
                {
                    if (!string.IsNullOrEmpty(fileName))
                    {
                        string oldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/Prescription", fileName);

                        if (System.IO.File.Exists(oldPath))
                        {
                            System.IO.File.Delete(oldPath);
                        }
                    }

                    string extension = Path.GetExtension(model.Prescription.FileName);

                    fileName = Guid.NewGuid().ToString() + extension;

                    string folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/Prescription");

                    if (!Directory.Exists(folder))
                    {
                        Directory.CreateDirectory(folder);
                    }

                    string filePath = Path.Combine(folder, fileName);

                    using (FileStream stream = new FileStream(filePath, FileMode.Create))
                    {
                        model.Prescription.CopyTo(stream);
                    }
                }

                SqlCommand cmd = new SqlCommand(@"
        UPDATE tbl_Appointment
        SET PrescriptionFile=@PrescriptionFile,
            UpdatedDate=@UpdatedDate
        WHERE AppointmentId=@AppointmentId", con);

                cmd.Parameters.AddWithValue("@PrescriptionFile", fileName);
                cmd.Parameters.AddWithValue("@UpdatedDate", DateTime.Now);
                cmd.Parameters.AddWithValue("@AppointmentId", model.AppointmentId);

                int result = cmd.ExecuteNonQuery();

                if (result > 0)
                {
                    TempData["Success"] = "Prescription uploaded successfully.";
                }
                else
                {
                    TempData["Error"] = "Something went wrong.";
                }

                con.Close();
            }

            return RedirectToAction("ManageAppointments");
        }
        [HttpGet]
        public IActionResult CompleteAppointment(long id)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            using (SqlConnection con = new SqlConnection(cs))
            {
                con.Open();

                SqlCommand checkCmd = new SqlCommand("SELECT AppointmentStatus FROM tbl_Appointment WHERE AppointmentId=@AppointmentId", con);
                checkCmd.Parameters.AddWithValue("@AppointmentId", id);

                object statusObj = checkCmd.ExecuteScalar();

                if (statusObj == null)
                {
                    TempData["Error"] = "Appointment not found.";
                    return RedirectToAction("ManageAppointments");
                }

                string status = statusObj.ToString();

                if (status == "Completed")
                {
                    TempData["Error"] = "Appointment is already completed.";
                    return RedirectToAction("ManageAppointments");
                }

                if (status == "Cancelled")
                {
                    TempData["Error"] = "Cancelled appointment cannot be completed.";
                    return RedirectToAction("ManageAppointments");
                }

                if (status == "Rejected")
                {
                    TempData["Error"] = "Rejected appointment cannot be completed.";
                    return RedirectToAction("ManageAppointments");
                }

                if (status != "Approved")
                {
                    TempData["Error"] = "Only approved appointments can be completed.";
                    return RedirectToAction("ManageAppointments");
                }

                SqlCommand cmd = new SqlCommand(@"UPDATE tbl_Appointment
                                          SET AppointmentStatus=@AppointmentStatus,
                                              UpdatedDate=@UpdatedDate
                                          WHERE AppointmentId=@AppointmentId", con);

                cmd.Parameters.AddWithValue("@AppointmentStatus", "Completed");
                cmd.Parameters.AddWithValue("@UpdatedDate", DateTime.Now);
                cmd.Parameters.AddWithValue("@AppointmentId", id);

                cmd.ExecuteNonQuery();

                TempData["Success"] = "Appointment completed successfully.";
            }

            return RedirectToAction("ManageAppointments");
        }
        // ===========================
        // Logout
        // ===========================
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}
