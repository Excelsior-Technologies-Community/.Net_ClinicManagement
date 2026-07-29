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