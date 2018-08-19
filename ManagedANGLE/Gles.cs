using System;
using System.Runtime.InteropServices;
using System.Text;

using GLbitfield = System.UInt32;
using GLboolean = System.Byte;
using GLenum = System.UInt32;
using GLfloat = System.Single;
using GLint = System.Int32;
using GLintptr = System.Int32;
using GLsizei = System.Int32;
using GLsizeiptr = System.Int32;
using GLuint = System.UInt32;

namespace ManagedANGLE
{
    internal static class Gles
    {
#pragma warning disable IDE1006 // Naming Styles

        private const string libGLESv2 = "libGLESv2.dll";

        public const GLenum GL_DEPTH_BUFFER_BIT = 0x00000100;
        public const GLenum GL_STENCIL_BUFFER_BIT = 0x00000400;
        public const GLenum GL_COLOR_BUFFER_BIT = 0x00004000;

        public const GLboolean GL_FALSE = 0;
        public const GLboolean GL_TRUE = 1;

        public const GLenum GL_POINTS = 0x0000;
        public const GLenum GL_LINES = 0x0001;
        public const GLenum GL_LINE_LOOP = 0x0002;
        public const GLenum GL_LINE_STRIP = 0x0003;
        public const GLenum GL_TRIANGLES = 0x0004;
        public const GLenum GL_TRIANGLE_STRIP = 0x0005;
        public const GLenum GL_TRIANGLE_FAN = 0x0006;

        public const GLenum GL_BUFFER_SIZE = 0x8764;
        public const GLenum GL_BUFFER_USAGE = 0x8765;

        public const GLenum GL_ARRAY_BUFFER = 0x8892;
        public const GLenum GL_ELEMENT_ARRAY_BUFFER = 0x8893;

        public const GLenum GL_STREAM_DRAW = 0x88E0;
        public const GLenum GL_STATIC_DRAW = 0x88E4;
        public const GLenum GL_DYNAMIC_DRAW = 0x88E8;

        public const GLenum GL_COMPILE_STATUS = 0x8B81;
        public const GLenum GL_LINK_STATUS = 0x8B82;

        public const GLenum GL_INFO_LOG_LENGTH = 0x8B84;

        public const GLenum GL_FRAGMENT_SHADER = 0x8B30;
        public const GLenum GL_VERTEX_SHADER = 0x8B31;

        public const GLenum GL_STENCIL_TEST = 0x0B90;
        public const GLenum GL_DEPTH_TEST = 0x0B71;
        public const GLenum GL_SCISSOR_TEST = 0x0C11;

        public const GLenum GL_BYTE = 0x1400;
        public const GLenum GL_UNSIGNED_BYTE = 0x1401;
        public const GLenum GL_SHORT = 0x1402;
        public const GLenum GL_UNSIGNED_SHORT = 0x1403;
        public const GLenum GL_INT = 0x1404;
        public const GLenum GL_UNSIGNED_INT = 0x1405;
        public const GLenum GL_FLOAT = 0x1406;
        public const GLenum GL_FIXED = 0x140C;

        [DllImport(libGLESv2)]
        public static extern GLenum glGetError();

        public static void glThrowError()
        {
            var error = Gles.glGetError();

            if (error != 0)
                throw new Exception("OpenGL ES error: 0x" + error.ToString("x").PadLeft(4, '0'));
        }


        [DllImport(libGLESv2)]
        public static extern GLuint glCreateProgram();

        [DllImport(libGLESv2)]
        public static extern void glDeleteProgram(GLuint program);

        [DllImport(libGLESv2)]
        public static extern void glLinkProgram(GLuint program);

        [DllImport(libGLESv2)]
        public static extern void glGetProgramiv(GLuint program, GLenum pname, out GLint param);

        [DllImport(libGLESv2)]
        public static extern void glUseProgram(GLuint program);

        [DllImport(libGLESv2)]
        public static extern void glGetProgramInfoLog(GLuint program, GLsizei maxLength, out GLsizei length, StringBuilder infoLog);

        [DllImport(libGLESv2)]
        public static extern GLint glGetAttribLocation(GLuint program, String name);

        [DllImport(libGLESv2)]
        public static extern GLint glGetUniformLocation(GLuint program, String name);

        [DllImport(libGLESv2)]
        public static extern void glAttachShader(GLuint program, GLuint shader);

        [DllImport(libGLESv2)]
        public static extern void glGenBuffers(GLsizei n, out GLuint buffers);

        [DllImport(libGLESv2)]
        public static extern void glDeleteBuffers(GLsizei n, ref GLuint buffers);

        [DllImport(libGLESv2)]
        public static extern void glBindBuffer(GLenum target, GLuint buffer);

        [DllImport(libGLESv2)]
        public static extern void glBufferSubData(GLenum target, GLintptr offset, GLsizeiptr size, IntPtr data);

        public static void glBufferSubData<T>(GLenum target, GLintptr offset, T[] data)
        {
            var gcData = GCHandle.Alloc(data, GCHandleType.Pinned);
            try
            {
                glBufferSubData(target, offset, data.Length * Marshal.SizeOf<T>(), gcData.AddrOfPinnedObject());
            }
            finally
            {
                gcData.Free();
            }
        }

        [DllImport(libGLESv2)]
        public static extern void glBufferData(GLenum target, GLsizeiptr size, IntPtr data, GLenum usage);

        public static void glBufferData<T>(GLenum target, T[] data, GLenum usage)
        {
            var gcData = GCHandle.Alloc(data, GCHandleType.Pinned);
            try
            {
                glBufferData(target, data.Length * Marshal.SizeOf<T>(), gcData.AddrOfPinnedObject(), usage);
            }
            finally
            {
                gcData.Free();
            }
        }

        [DllImport(libGLESv2)]
        public static extern void glGetBufferParameteriv(GLenum target, GLenum pname, out GLint param);

        [DllImport(libGLESv2)]
        public static extern GLuint glCreateShader(GLenum type);

        [DllImport(libGLESv2)]
        public static extern void glDeleteShader(GLuint shader);

        [DllImport(libGLESv2)]
        public static extern void glShaderSource(GLuint shader, GLsizei count, String[] source, GLint[] length);

        [DllImport(libGLESv2)]
        public static extern void glCompileShader(GLuint shader);

        [DllImport(libGLESv2)]
        public static extern void glGetShaderiv(GLuint shader, GLenum pname, out GLint param);

        [DllImport(libGLESv2)]
        public static extern void glGetShaderInfoLog(GLuint shader, GLsizei maxLength, out GLsizei length, StringBuilder infoLog);

        [DllImport(libGLESv2)]
        public static extern void glViewport(GLint x, GLint y, GLsizei width, GLsizei height);

        [DllImport(libGLESv2)]
        public static extern void glEnable(GLenum cap);

        [DllImport(libGLESv2)]
        public static extern void glClear(GLbitfield mask);

        [DllImport(libGLESv2)]
        public static extern void glClearColor(GLfloat red, GLfloat green, GLfloat blue, GLfloat alpha);

        [DllImport(libGLESv2)]
        public static extern void glDrawElements(GLenum mode, GLsizei count, GLenum type, IntPtr indices);

        [DllImport(libGLESv2)]
        public static extern void glDrawArrays(GLenum mode, GLint first, GLsizei count);

        [DllImport(libGLESv2)]
        public static extern void glEnableVertexAttribArray(GLuint index);

        [DllImport(libGLESv2)]
        public static extern void glUniformMatrix4fv(GLint location, GLsizei count, GLboolean transpose, GLfloat[,] value);

        [DllImport(libGLESv2)]
        public static extern void glVertexAttribPointer(GLuint index, GLint size, GLenum type, GLboolean normalized, GLsizei stride, IntPtr pointer);

        public static void glVertexAttribPointer<T>(GLuint index, GLint size, GLenum type, GLboolean normalized, GLsizei stride, T[] pointer)
        {
            var gcData = GCHandle.Alloc(pointer, GCHandleType.Pinned);
            try
            {
                glVertexAttribPointer(index, size, type, normalized, stride, gcData.AddrOfPinnedObject());
            }
            finally
            {
                gcData.Free();
            }
        }

#pragma warning restore IDE1006 // Naming Styles
    }
}
