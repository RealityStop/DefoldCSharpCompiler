-- Generated by CSharp.lua Compiler 1.1.0
--[[
Copyright 2017 YANG Huan (sy.yanghuan@gmail.com).

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

  http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
]]
local System = System
local CSharpLuaLuaAst
System.usingDeclare(function (global) 
  CSharpLuaLuaAst = CSharpLua.LuaAst
end)
System.namespace("CSharpLua.LuaAst", function (namespace) 
  namespace.class("LuaParameterListSyntax", function (namespace) 
    local getOpenParenToken, getCloseParenToken, Render, __init__, __ctor__
    __init__ = function (this) 
      this.Parameters = CSharpLuaLuaAst.LuaSyntaxList_1(CSharpLuaLuaAst.LuaParameterSyntax)()
    end
    __ctor__ = function (this) 
      __init__(this)
      this.__base__.__ctor__(this)
    end
    getOpenParenToken = function (this) 
      return "(" --[[Tokens.OpenParentheses]]
    end
    getCloseParenToken = function (this) 
      return ")" --[[Tokens.CloseParentheses]]
    end
    Render = function (this, renderer) 
      renderer:Render10(this)
    end
    return {
      __inherits__ = function (global) 
        return {
          global.CSharpLua.LuaAst.LuaSyntaxNode
        }
      end, 
      getOpenParenToken = getOpenParenToken, 
      getCloseParenToken = getCloseParenToken, 
      Render = Render, 
      __ctor__ = __ctor__
    }
  end)

  namespace.class("LuaParameterSyntax", function (namespace) 
    local Render, __ctor__
    __ctor__ = function (this, identifier) 
      this.__base__.__ctor__(this)
      this.Identifier = identifier
    end
    Render = function (this, renderer) 
      renderer:Render11(this)
    end
    return {
      __inherits__ = function (global) 
        return {
          global.CSharpLua.LuaAst.LuaSyntaxNode
        }
      end, 
      Render = Render, 
      __ctor__ = __ctor__
    }
  end)
end)
