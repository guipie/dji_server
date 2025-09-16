import request from '/@/utils/request';
enum Api {
  AddChatModel = '/api/chatModel/add',
  DeleteChatModel = '/api/chatModel/delete',
  UpdateChatModel = '/api/chatModel/update',
  PageChatModel = '/api/chatModel/page',
  DetailChatModel = '/api/chatModel/detail',
}

// 增加AIModels
export const addChatModel = (params?: any) =>
	request({
		url: Api.AddChatModel,
		method: 'post',
		data: params,
	});

// 删除AIModels
export const deleteChatModel = (params?: any) => 
	request({
			url: Api.DeleteChatModel,
			method: 'post',
			data: params,
		});

// 编辑AIModels
export const updateChatModel = (params?: any) => 
	request({
			url: Api.UpdateChatModel,
			method: 'post',
			data: params,
		});

// 分页查询AIModels
export const pageChatModel = (params?: any) => 
	request({
			url: Api.PageChatModel,
			method: 'post',
			data: params,
		});

// 详情AIModels
export const detailChatModel = (id: any) => 
	request({
			url: Api.DetailChatModel,
			method: 'get',
			data: { id },
		});


