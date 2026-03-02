from pydantic import BaseModel
from typing import Optional, List
from enum import IntEnum

class UserRole(IntEnum):
    Admin = 0
    Teacher = 1
    Student = 2

class SubjectBase(BaseModel):
    Name: str

class SubjectCreate(SubjectBase):
    pass

class Subject(SubjectBase):
    Id: int

    class Config:
        from_attributes = True

class ClassroomBase(BaseModel):
    Number: str
    Capacity: Optional[int] = None
    Type: Optional[str] = None

class ClassroomCreate(ClassroomBase):
    pass

class Classroom(ClassroomBase):
    Id: int

    class Config:
        from_attributes = True

class TeacherBase(BaseModel):
    FullName: str
    SubjectId: Optional[int] = None
    ClassroomId: Optional[int] = None

class TeacherCreate(TeacherBase):
    pass

class Teacher(TeacherBase):
    Id: int
    Subject: Optional[Subject] = None
    Classroom: Optional[Classroom] = None

    class Config:
        from_attributes = True

class AcademicClassBase(BaseModel):
    Name: str
    StudentCount: Optional[int] = None
    Shift: Optional[int] = None
    CuratorTeacherId: Optional[int] = None

class AcademicClassCreate(AcademicClassBase):
    pass

class AcademicClass(AcademicClassBase):
    Id: int
    CuratorTeacher: Optional[Teacher] = None

    class Config:
        from_attributes = True

class WorkloadBase(BaseModel):
    TeacherId: int
    SubjectId: int
    AcademicClassId: int
    HoursPerWeek: int

class WorkloadCreate(WorkloadBase):
    pass

class Workload(WorkloadBase):
    Id: int
    Teacher: Optional[Teacher] = None
    Subject: Optional[Subject] = None
    AcademicClass: Optional[AcademicClass] = None

    class Config:
        from_attributes = True

class LessonBase(BaseModel):
    DayOfWeek: int
    LessonIndex: int
    TeacherId: int
    SubjectId: int
    AcademicClassId: int
    ClassroomId: Optional[int] = None

class LessonCreate(LessonBase):
    pass

class Lesson(LessonBase):
    Id: int
    Teacher: Optional[Teacher] = None
    Subject: Optional[Subject] = None
    AcademicClass: Optional[AcademicClass] = None
    Classroom: Optional[Classroom] = None

    class Config:
        from_attributes = True

class UserBase(BaseModel):
    Username: str
    FullName: str
    Role: Optional[UserRole] = UserRole.Student
    TeacherId: Optional[int] = None
    AcademicClassId: Optional[int] = None

class UserCreate(UserBase):
    Password: str

class User(UserBase):
    Id: int
    Teacher: Optional[Teacher] = None
    AcademicClass: Optional[AcademicClass] = None

    class Config:
        from_attributes = True
